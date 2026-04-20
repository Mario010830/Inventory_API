using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class SaleReturnService : ISaleReturnService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<ISaleReturnService> _localizer;
        private readonly IInventorySettings _inventorySettings;
        private readonly ILoyaltyService _loyaltyService;

        public SaleReturnService(
            IUnitOfWork uow,
            CoreDbContext context,
            IStringLocalizer<ISaleReturnService> localizer,
            IInventorySettings inventorySettings,
            ILoyaltyService loyaltyService)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
            _inventorySettings = inventorySettings;
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        }

        public async Task<SaleReturn> CreateSaleReturn(CreateSaleReturnRequest request, int userId)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var saleOrder = await _context.SaleOrders
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.StockParentProduct)
                .FirstOrDefaultAsync(s => s.Id == request.SaleOrderId);

            if (saleOrder == null)
                throw new SaleOrderNotFoundException(_localizer);

            if (saleOrder.Status == SaleOrderStatus.returned)
                throw new SaleOrderFullyReturnedBadRequestException(_localizer);

            if (saleOrder.Status != SaleOrderStatus.confirmed)
                throw new SaleOrderNotConfirmedBadRequestException(_localizer);

            var decimals = _inventorySettings.RoundingDecimals;
            var priceDecimals = _inventorySettings.PriceRoundingDecimals;

            var saleReturn = new SaleReturn
            {
                OrganizationId = orgId,
                SaleOrderId = request.SaleOrderId,
                LocationId = saleOrder.LocationId,
                Status = SaleReturnStatus.completed,
                Reason = request.Reason,
                Notes = request.Notes,
                UserId = userId,
            };

            decimal total = 0;

            foreach (var itemReq in request.Items)
            {
                var originalItem = saleOrder.Items.FirstOrDefault(i => i.Id == itemReq.SaleOrderItemId);
                if (originalItem == null)
                    throw new SaleOrderNotFoundException(_localizer);

                // Calcular cuánto ya fue devuelto de este item en devoluciones anteriores
                var alreadyReturned = await _uow.SaleReturnItemRepository
                    .GetAll()
                    .Where(ri => ri.SaleOrderItemId == itemReq.SaleOrderItemId)
                    .SumAsync(ri => ri.Quantity);

                var availableToReturn = originalItem.Quantity - alreadyReturned;
                var qtyToReturn = DecimalRoundingHelper.RoundQuantity(itemReq.Quantity, decimals);

                if (qtyToReturn > availableToReturn)
                    throw new SaleReturnQuantityExceedsBadRequestException(_localizer);

                var lineTotal = Math.Round(qtyToReturn * originalItem.UnitPrice, priceDecimals);

                saleReturn.Items.Add(new SaleReturnItem
                {
                    SaleOrderItemId = itemReq.SaleOrderItemId,
                    ProductId = originalItem.ProductId,
                    Quantity = qtyToReturn,
                    UnitPrice = originalItem.UnitPrice,
                    LineTotal = lineTotal,
                });

                total += lineTotal;

                if (originalItem.Product == null)
                    throw new ProductNotFoundException(_localizer);
                var lineProduct = originalItem.Product;
                var (stockProductId, stockQty) = ProductStockResolution.GetDeductionUnits(lineProduct, qtyToReturn, decimals);

                // Reponer inventario (producto padre si la línea consume stock padre)
                var inventory = await _uow.InventoryRepository.FirstOrDefaultAsync(
                    i => i.ProductId == stockProductId && i.LocationId == saleOrder.LocationId);

                decimal previousStock = inventory?.CurrentStock ?? 0;
                decimal newStock = DecimalRoundingHelper.RoundQuantity(previousStock + stockQty, decimals);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = stockProductId,
                        LocationId = saleOrder.LocationId,
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

                var movementUnitCost = originalItem.UnitCost;
                if (ProductStockResolution.UsesParentStock(lineProduct) && lineProduct.StockParentProduct != null)
                    movementUnitCost = Math.Round(lineProduct.StockParentProduct.Costo, priceDecimals);

                // Crear movimiento de entrada por devolución
                var movement = new InventoryMovement
                {
                    ProductId = stockProductId,
                    LocationId = saleOrder.LocationId,
                    Type = InventoryMovementType.entry,
                    Reason = InventoryMovementReason.DevolucionCliente.ToString(),
                    Quantity = stockQty,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    UnitCost = movementUnitCost,
                    UnitPrice = originalItem.UnitPrice,
                    SaleOrderId = saleOrder.Id,
                    ReferenceDocument = $"DEV-{saleOrder.Folio}",
                    UserId = userId,
                };
                await _uow.InventoryMovementRepository.AddAsync(movement);
            }

            saleReturn.Total = Math.Round(total, priceDecimals);

            if (await IsSaleOrderFullyReturnedAfterThisReturnAsync(saleOrder, saleReturn, decimals))
            {
                saleOrder.Status = SaleOrderStatus.returned;
                saleOrder.ModifiedAt = DateTime.UtcNow;
                _uow.SaleOrderRepository.Update(saleOrder);
                await _loyaltyService.ProcessFullyReturnedSaleOrderAsync(saleOrder);
            }

            await _uow.SaleReturnRepository.AddAsync(saleReturn);
            await _uow.CommitAsync();

            return await LoadFullReturn(saleReturn.Id);
        }

        public async Task<SaleReturn> GetSaleReturn(int id)
        {
            var result = await LoadFullReturn(id);
            if (result == null)
                throw new SaleReturnNotFoundException(_localizer);
            return result;
        }

        public async Task<PaginatedList<SaleReturn>> GetAllSaleReturns(int? page, int? perPage, string? sortOrder)
        {
            var query = _uow.SaleReturnRepository
                .GetAllIncluding(r => r.Items, r => r.SaleOrder, r => r.Location);
            return await PaginatedList<SaleReturn>.CreateAsync(query, page ?? 1, perPage ?? 10);
        }

        public async Task<PaginatedList<SaleReturn>> GetReturnsBySaleOrder(int saleOrderId, int? page, int? perPage)
        {
            var query = _uow.SaleReturnRepository
                .GetAllIncluding(r => r.Items, r => r.Location)
                .Where(r => r.SaleOrderId == saleOrderId);
            return await PaginatedList<SaleReturn>.CreateAsync(query, page ?? 1, perPage ?? 10);
        }

        private async Task<SaleReturn> LoadFullReturn(int id)
        {
            return await _uow.SaleReturnRepository
                .GetAllIncluding(r => r.Items, r => r.SaleOrder, r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// Devolución total: cada línea del pedido queda con cantidad devuelta &gt;= cantidad vendida (incluida esta devolución).
        /// </summary>
        private async Task<bool> IsSaleOrderFullyReturnedAfterThisReturnAsync(SaleOrder saleOrder, SaleReturn pendingReturn, int decimals)
        {
            foreach (var line in saleOrder.Items)
            {
                var returnedBefore = await _uow.SaleReturnItemRepository
                    .GetAll()
                    .Where(ri => ri.SaleOrderItemId == line.Id)
                    .SumAsync(ri => ri.Quantity);

                returnedBefore = DecimalRoundingHelper.RoundQuantity(returnedBefore, decimals);
                var qtyThisBatch = pendingReturn.Items
                    .Where(i => i.SaleOrderItemId == line.Id)
                    .Sum(i => i.Quantity);
                qtyThisBatch = DecimalRoundingHelper.RoundQuantity(qtyThisBatch, decimals);
                var lineQty = DecimalRoundingHelper.RoundQuantity(line.Quantity, decimals);

                if (returnedBefore + qtyThisBatch < lineQty)
                    return false;
            }

            return true;
        }
    }
}
