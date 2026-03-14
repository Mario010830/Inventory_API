using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class SaleOrderService : ISaleOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<ISaleOrderService> _localizer;
        private readonly IInventorySettings _inventorySettings;

        public SaleOrderService(
            IUnitOfWork uow,
            CoreDbContext context,
            IStringLocalizer<ISaleOrderService> localizer,
            IInventorySettings inventorySettings)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
            _inventorySettings = inventorySettings;
        }

        public async Task<SaleOrder> CreateSaleOrder(CreateSaleOrderRequest request, int userId)
        {
            // IgnoreQueryFilters: permite que invitados (sin login) creen órdenes; la location se busca por id sin filtro de tenant.
            var location = await _context.Locations.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == request.LocationId);
            if (location == null)
                throw new LocationNotFoundException(_localizer);

            // Usuario autenticado: usar su organización. Invitado: usar la organización de la ubicación elegida.
            var orgId = _context.CurrentOrganizationId > 0
                ? _context.CurrentOrganizationId
                : location.OrganizationId;

            if (request.ContactId.HasValue)
            {
                var contact = await _uow.ContactRepository.FirstOrDefaultAsync(c => c.Id == request.ContactId.Value);
                if (contact == null)
                    throw new ContactNotFoundException(_localizer);
            }

            var decimals = _inventorySettings.RoundingDecimals;
            var priceDecimals = _inventorySettings.PriceRoundingDecimals;

            var order = new SaleOrder
            {
                OrganizationId = orgId,
                LocationId = request.LocationId,
                ContactId = request.ContactId,
                Status = SaleOrderStatus.draft,
                Notes = request.Notes,
                DiscountAmount = Math.Round(request.DiscountAmount, priceDecimals),
                UserId = userId > 0 ? userId : null,
            };

            decimal subtotal = 0;
            foreach (var itemReq in request.Items)
            {
                // IgnoreQueryFilters: invitados sin login; el producto debe ser de la org de la ubicación.
                var product = await _context.Products.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == itemReq.ProductId && p.OrganizationId == orgId);
                if (product == null)
                    throw new ProductNotFoundException(_localizer);

                var qty = DecimalRoundingHelper.RoundQuantity(itemReq.Quantity, decimals);
                var unitPrice = Math.Round(itemReq.UnitPrice ?? product.Precio, priceDecimals);
                var unitCost = Math.Round(product.Costo, priceDecimals);
                var discount = Math.Round(itemReq.Discount, priceDecimals);
                var lineTotal = Math.Round(qty * unitPrice - discount, priceDecimals);

                order.Items.Add(new SaleOrderItem
                {
                    ProductId = itemReq.ProductId,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    UnitCost = unitCost,
                    Discount = discount,
                    LineTotal = lineTotal,
                });

                subtotal += lineTotal;
            }

            order.Subtotal = Math.Round(subtotal, priceDecimals);
            order.Total = Math.Round(order.Subtotal - order.DiscountAmount, priceDecimals);
            order.Folio = await GenerateFolioAsync(orgId);

            await _uow.SaleOrderRepository.AddAsync(order);
            await _uow.CommitAsync();

            return await LoadFullOrder(order.Id);
        }

        public async Task<SaleOrder> ConfirmSaleOrder(int id, int userId)
        {
            var order = await LoadFullOrder(id);
            if (order == null)
                throw new SaleOrderNotFoundException(_localizer);

            if (order.Status == SaleOrderStatus.cancelled)
                throw new SaleOrderAlreadyCancelledBadRequestException(_localizer);

            if (order.Status == SaleOrderStatus.confirmed)
                return order;

            var allowNegative = _inventorySettings.AllowNegativeStock;
            var decimals = _inventorySettings.RoundingDecimals;

            // Validar stock antes de confirmar
            foreach (var item in order.Items)
            {
                var inventory = await _uow.InventoryRepository.FirstOrDefaultAsync(
                    i => i.ProductId == item.ProductId && i.LocationId == order.LocationId);

                if (inventory == null && !allowNegative)
                    throw new InsufficientStockBadRequestException(_localizer);

                if (inventory != null && !allowNegative && inventory.CurrentStock < item.Quantity)
                    throw new InsufficientStockBadRequestException(_localizer);
            }

            // Descontar inventario y crear movimientos
            foreach (var item in order.Items)
            {
                var inventory = await _uow.InventoryRepository.FirstOrDefaultAsync(
                    i => i.ProductId == item.ProductId && i.LocationId == order.LocationId);

                decimal previousStock = inventory?.CurrentStock ?? 0;
                decimal newStock = DecimalRoundingHelper.RoundQuantity(previousStock - item.Quantity, decimals);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = item.ProductId,
                        LocationId = order.LocationId,
                        CurrentStock = newStock,
                        MinimumStock = 0,
                        UnitOfMeasure = _inventorySettings.DefaultUnitOfMeasure,
                    };
                    await _uow.InventoryRepository.AddAsync(inventory);
                }
                else
                {
                    inventory.CurrentStock = newStock;
                    _uow.InventoryRepository.Update(inventory);
                }

                var movement = new InventoryMovement
                {
                    ProductId = item.ProductId,
                    LocationId = order.LocationId,
                    Type = InventoryMovementType.exit,
                    Reason = InventoryMovementReason.Venta,
                    Quantity = item.Quantity,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    UnitCost = item.UnitCost,
                    UnitPrice = item.UnitPrice,
                    SaleOrderId = order.Id,
                    ReferenceDocument = order.Folio,
                    UserId = userId,
                };
                await _uow.InventoryMovementRepository.AddAsync(movement);
            }

            order.Status = SaleOrderStatus.confirmed;
            _uow.SaleOrderRepository.Update(order);
            await _uow.CommitAsync();

            return await LoadFullOrder(id);
        }

        public async Task<SaleOrder> CancelSaleOrder(int id, int userId)
        {
            var order = await LoadFullOrder(id);
            if (order == null)
                throw new SaleOrderNotFoundException(_localizer);

            if (order.Status == SaleOrderStatus.cancelled)
                throw new SaleOrderAlreadyCancelledBadRequestException(_localizer);

            // Si ya estaba confirmada, hay que revertir el inventario
            if (order.Status == SaleOrderStatus.confirmed)
            {
                var decimals = _inventorySettings.RoundingDecimals;

                foreach (var item in order.Items)
                {
                    var inventory = await _uow.InventoryRepository.FirstOrDefaultAsync(
                        i => i.ProductId == item.ProductId && i.LocationId == order.LocationId);

                    decimal previousStock = inventory?.CurrentStock ?? 0;
                    decimal newStock = DecimalRoundingHelper.RoundQuantity(previousStock + item.Quantity, decimals);

                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            ProductId = item.ProductId,
                            LocationId = order.LocationId,
                            CurrentStock = newStock,
                            MinimumStock = 0,
                            UnitOfMeasure = _inventorySettings.DefaultUnitOfMeasure,
                        };
                        await _uow.InventoryRepository.AddAsync(inventory);
                    }
                    else
                    {
                        inventory.CurrentStock = newStock;
                        _uow.InventoryRepository.Update(inventory);
                    }

                    // Movimiento de reversión
                    var reversal = new InventoryMovement
                    {
                        ProductId = item.ProductId,
                        LocationId = order.LocationId,
                        Type = InventoryMovementType.entry,
                        Reason = InventoryMovementReason.Correccion,
                        Quantity = item.Quantity,
                        PreviousStock = previousStock,
                        NewStock = newStock,
                        UnitCost = item.UnitCost,
                        UnitPrice = item.UnitPrice,
                        SaleOrderId = order.Id,
                        ReferenceDocument = $"CANCEL-{order.Folio}",
                        UserId = userId,
                    };
                    await _uow.InventoryMovementRepository.AddAsync(reversal);
                }
            }

            order.Status = SaleOrderStatus.cancelled;
            _uow.SaleOrderRepository.Update(order);
            await _uow.CommitAsync();

            return await LoadFullOrder(id);
        }

        public async Task<SaleOrder> UpdateSaleOrder(int id, UpdateSaleOrderRequest request)
        {
            var order = await _uow.SaleOrderRepository.FirstOrDefaultAsync(s => s.Id == id);
            if (order == null)
                throw new SaleOrderNotFoundException(_localizer);

            if (order.Status != SaleOrderStatus.draft)
                throw new SaleOrderCannotEditBadRequestException(_localizer);

            if (request.ContactId.HasValue)
            {
                var contact = await _uow.ContactRepository.FirstOrDefaultAsync(c => c.Id == request.ContactId.Value);
                if (contact == null)
                    throw new ContactNotFoundException(_localizer);
                order.ContactId = request.ContactId.Value;
            }

            if (request.Notes != null) order.Notes = request.Notes;
            if (request.DiscountAmount.HasValue)
            {
                order.DiscountAmount = Math.Round(request.DiscountAmount.Value, _inventorySettings.PriceRoundingDecimals);
                order.Total = Math.Round(order.Subtotal - order.DiscountAmount, _inventorySettings.PriceRoundingDecimals);
            }

            _uow.SaleOrderRepository.Update(order);
            await _uow.CommitAsync();

            return await LoadFullOrder(id);
        }

        public async Task<SaleOrder> GetSaleOrder(int id)
        {
            var order = await LoadFullOrder(id);
            if (order == null)
                throw new SaleOrderNotFoundException(_localizer);
            return order;
        }

        public async Task<PaginatedList<SaleOrder>> GetAllSaleOrders(int? page, int? perPage, string? status, string? sortOrder)
        {
            var query = _uow.SaleOrderRepository
                .GetAllIncluding(s => s.Items, s => s.Contact, s => s.Location);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SaleOrderStatus>(status, true, out var statusEnum))
                query = query.Where(s => s.Status == statusEnum);

            return await PaginatedList<SaleOrder>.CreateAsync(query, page ?? 1, perPage ?? 10);
        }

        public async Task<SaleStatsResponse> GetStats(int? days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-(days ?? 30));

            var orders = await _uow.SaleOrderRepository
                .GetAllIncluding(s => s.Items)
                .Where(s => s.Status == SaleOrderStatus.confirmed && s.CreatedAt >= cutoff)
                .ToListAsync();

            var returns = await _uow.SaleReturnRepository
                .GetAll()
                .Where(r => r.Status == SaleReturnStatus.completed && r.CreatedAt >= cutoff)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var revenue = orders.Sum(o => o.Total);
            var cogs = orders.SelectMany(o => o.Items).Sum(i => i.UnitCost * i.Quantity);
            var grossMargin = revenue - cogs;

            return new SaleStatsResponse
            {
                TotalSales = orders.Count,
                SalesToday = orders.Count(o => o.CreatedAt.Date == today),
                TotalRevenue = revenue,
                TotalCogs = cogs,
                GrossMargin = grossMargin,
                GrossMarginPercent = revenue > 0 ? Math.Round(grossMargin / revenue * 100, 2) : 0,
                TotalReturns = returns.Count,
                TotalReturnsAmount = returns.Sum(r => r.Total),
            };
        }

        private async Task<SaleOrder> LoadFullOrder(int id)
        {
            // Cargar orden con todos sus datos relacionados (items + producto, contacto, ubicación)
            return await _context.SaleOrders
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .Include(s => s.Contact)
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        private async Task<string> GenerateFolioAsync(int orgId)
        {
            var count = await _uow.SaleOrderRepository.GetAll()
                .Where(s => s.OrganizationId == orgId)
                .CountAsync();
            return $"VENTA-{(count + 1):D4}";
        }
    }
}
