using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStringLocalizer<IInventoryService> _localizer;
        private readonly IInventorySettings _inventorySettings;

        public InventoryService(IUnitOfWork uow, IStringLocalizer<IInventoryService> localizer, IInventorySettings inventorySettings)
        {
            _uow = uow;
            _localizer = localizer;
            _inventorySettings = inventorySettings ?? throw new ArgumentNullException(nameof(inventorySettings));
        }

        public async Task<Inventory> CreateInventory(CreateInventoryRequest request)
        {
            var productExists = await _uow.ProductRepository.FirstOrDefaultAsync(p => p.Id == request.ProductId);
            if (productExists == null)
            {
                throw new ProductNotFoundException(_localizer);
            }

            // Opción A: se permiten varios inventarios por (producto, ubicación) para lotes/entradas distintas.
            var decimals = _inventorySettings.RoundingDecimals;
            var currentStock = DecimalRoundingHelper.RoundQuantity(request.CurrentStock, decimals);
            var minimumStock = DecimalRoundingHelper.RoundQuantity(request.MinimumStock, decimals);
            var unitOfMeasure = !string.IsNullOrWhiteSpace(request.UnitOfMeasure) ? request.UnitOfMeasure.Trim() : _inventorySettings.DefaultUnitOfMeasure;

            var newInventory = new Inventory
            {
                ProductId = request.ProductId,
                LocationId = request.LocationId,
                CurrentStock = currentStock,
                MinimumStock = minimumStock,
                UnitOfMeasure = unitOfMeasure,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            await _uow.InventoryRepository.AddAsync(newInventory);
            await _uow.CommitAsync();

            return newInventory;
        }

        public async Task DeleteInventory(int id)
        {
            var inventory = await _uow.InventoryRepository.FirstOrDefaultAsync(i => i.Id == id);
            if (inventory == null)
            {
                throw new InventoryNotFoundException(_localizer);
            }
            _uow.InventoryRepository.Delete(inventory);
            await _uow.CommitAsync();
        }

        public async Task<PaginatedList<Inventory>> GetAllInventories(int? page, int? perPage, string sortOrder = null)
        {
            var inventories = _uow.InventoryRepository.GetAllIncluding(i => i.Product, i => i.Location);
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<Inventory>.CreateAsync(inventories, pageIndex, perPageIndex);
        }

        public async Task<Inventory> GetInventory(int id)
        {
            var inventory = await _uow.InventoryRepository.GetAllIncluding(i => i.Product, i => i.Location)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inventory == null)
            {
                throw new InventoryNotFoundException(_localizer);
            }
            return inventory;
        }

        public async Task UpdateInventory(int id, UpdateInventoryRequest request)
        {
            var oldInventory = await _uow.InventoryRepository.FirstOrDefaultAsync(i => i.Id == id);
            if (oldInventory == null)
            {
                throw new InventoryNotFoundException(_localizer);
            }

            if (request.ProductId.HasValue)
            {
                var productExists = await _uow.ProductRepository.FirstOrDefaultAsync(p => p.Id == request.ProductId.Value);
                if (productExists == null)
                {
                    throw new ProductNotFoundException(_localizer);
                }
            }

            var decimals = _inventorySettings.RoundingDecimals;
            var currentStock = request.CurrentStock.HasValue ? DecimalRoundingHelper.RoundQuantity(request.CurrentStock.Value, decimals) : oldInventory.CurrentStock;
            var minimumStock = request.MinimumStock.HasValue ? DecimalRoundingHelper.RoundQuantity(request.MinimumStock.Value, decimals) : oldInventory.MinimumStock;
            var unitOfMeasure = !string.IsNullOrWhiteSpace(request.UnitOfMeasure) ? request.UnitOfMeasure.Trim() : oldInventory.UnitOfMeasure;

            var updatedInventory = new Inventory
            {
                Id = oldInventory.Id,
                CreatedAt = oldInventory.CreatedAt,
                ModifiedAt = DateTime.UtcNow,
                ProductId = request.ProductId ?? oldInventory.ProductId,
                LocationId = request.LocationId ?? oldInventory.LocationId,
                CurrentStock = currentStock,
                MinimumStock = minimumStock,
                UnitOfMeasure = unitOfMeasure,
            };

            await _uow.InventoryRepository.UpdateAsync(updatedInventory, oldInventory.Id);
            await _uow.CommitAsync();
        }

        public async Task<IEnumerable<ProductStockByLocationResponse>> GetStockByProductForLocation(int locationId)
        {
            var aggregated = await _uow.InventoryRepository.GetAll()
                .Where(i => i.LocationId == locationId)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(i => i.CurrentStock) })
                .ToListAsync();

            if (aggregated.Count == 0)
                return new List<ProductStockByLocationResponse>();

            var productIds = aggregated.Select(a => a.ProductId).Distinct().ToList();
            var products = await _uow.ProductRepository.GetAll()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();
            var productDict = products.ToDictionary(p => p.Id, p => p.Name ?? string.Empty);

            var decimals = _inventorySettings.RoundingDecimals;
            return aggregated.Select(a => new ProductStockByLocationResponse
            {
                ProductId = a.ProductId,
                ProductName = productDict.GetValueOrDefault(a.ProductId, string.Empty),
                TotalQuantity = DecimalRoundingHelper.RoundQuantity(a.TotalQuantity, decimals),
                UnitOfMeasure = _inventorySettings.DefaultUnitOfMeasure
            }).ToList();
        }
    }
}
