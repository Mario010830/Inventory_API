using APICore.Common.DTO.Request;
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
    public class SaleReturnService : ISaleReturnService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<ISaleReturnService> _localizer;
        private readonly IInventorySettings _inventorySettings;

        public SaleReturnService(
            IUnitOfWork uow,
            CoreDbContext context,
            IStringLocalizer<ISaleReturnService> localizer,
            IInventorySettings inventorySettings)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
            _inventorySettings = inventorySettings;
        }

        public async Task<SaleReturn> CreateSaleReturn(CreateSaleReturnRequest request, int userId)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var saleOrder = await _uow.SaleOrderRepository
                .GetAllIncluding(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == request.SaleOrderId);

            if (saleOrder == null)
                throw new SaleOrderNotFoundException(_localizer);

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

                // Reponer inventario
                var inventory = await _uow.InventoryRepository.FirstOrDefaultAsync(
                    i => i.ProductId == originalItem.ProductId && i.LocationId == saleOrder.LocationId);

                decimal previousStock = inventory?.CurrentStock ?? 0;
                decimal newStock = DecimalRoundingHelper.RoundQuantity(previousStock + qtyToReturn, decimals);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = originalItem.ProductId,
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

                // Crear movimiento de entrada por devolución
                var movement = new InventoryMovement
                {
                    ProductId = originalItem.ProductId,
                    LocationId = saleOrder.LocationId,
                    Type = InventoryMovementType.entry,
                    Reason = InventoryMovementReason.DevolucionCliente,
                    Quantity = qtyToReturn,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    UnitCost = originalItem.UnitCost,
                    UnitPrice = originalItem.UnitPrice,
                    SaleOrderId = saleOrder.Id,
                    ReferenceDocument = $"DEV-{saleOrder.Folio}",
                    UserId = userId,
                };
                await _uow.InventoryMovementRepository.AddAsync(movement);
            }

            saleReturn.Total = Math.Round(total, priceDecimals);

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
    }
}
