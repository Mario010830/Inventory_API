using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class InventoryMovementService : IInventoryMovementService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStringLocalizer<IInventoryMovementService> _localizer;
        private readonly IInventorySettings _inventorySettings;
        private readonly ISettingService _settingService;
        private readonly IEmailService _emailService;

        public InventoryMovementService(
            IUnitOfWork uow,
            IStringLocalizer<IInventoryMovementService> localizer,
            IInventorySettings inventorySettings,
            ISettingService settingService,
            IEmailService emailService)
        {
            _uow = uow;
            _localizer = localizer;
            _inventorySettings = inventorySettings ?? throw new ArgumentNullException(nameof(inventorySettings));
            _settingService = settingService;
            _emailService = emailService;
        }

        public async Task<InventoryMovement> CreateMovement(CreateInventoryMovementRequest request, int userId)
        {
            var product = await _uow.ProductRepository.FirstOrDefaultAsync(p => p.Id == request.ProductId);
            if (product == null)
                throw new ProductNotFoundException(_localizer);

            var inventory = await _uow.InventoryRepository.FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.LocationId == request.LocationId);
            if (inventory == null)
                throw new ProductHasNoInventoryBadRequestException(_localizer);

            if (!Enum.IsDefined(typeof(InventoryMovementType), request.Type))
                throw new InvalidMovementTypeBadRequestException(_localizer);

            var decimals = _inventorySettings.RoundingDecimals;
            var allowNegative = _inventorySettings.AllowNegativeStock;

            var quantity = DecimalRoundingHelper.RoundQuantity(request.Quantity, decimals);
            var movementType = (InventoryMovementType)request.Type;
            decimal previousStock = DecimalRoundingHelper.RoundQuantity(inventory.CurrentStock, decimals);
            decimal newStock;

            switch (movementType)
            {
                case InventoryMovementType.entry:
                    if (quantity <= 0)
                        throw new InvalidQuantityBadRequestException(_localizer);
                    newStock = previousStock + quantity;
                    break;
                case InventoryMovementType.exit:
                    if (quantity <= 0)
                        throw new InvalidQuantityBadRequestException(_localizer);
                    newStock = previousStock - quantity;
                    if (!allowNegative && newStock < 0)
                        throw new InsufficientStockBadRequestException(_localizer);
                    break;
                case InventoryMovementType.adjustment:
                    newStock = previousStock + quantity;
                    if (!allowNegative && newStock < 0)
                        throw new InsufficientStockBadRequestException(_localizer);
                    break;
                default:
                    throw new InvalidMovementTypeBadRequestException(_localizer);
            }

            newStock = DecimalRoundingHelper.RoundQuantity(newStock, decimals);

            if (request.SupplierId.HasValue)
            {
                var supplier = await _uow.SupplierRepository.FirstOrDefaultAsync(s => s.Id == request.SupplierId.Value);
                if (supplier == null)
                    throw new SupplierNotFoundException(_localizer);
            }

            var movement = new InventoryMovement
            {
                ProductId = request.ProductId,
                LocationId = request.LocationId,
                Type = movementType,
                Quantity = quantity,
                PreviousStock = previousStock,
                NewStock = newStock,
                UnitCost = null,
                UnitPrice = null,
                Reason = request.Reason,
                SupplierId = request.SupplierId,
                ReferenceDocument = request.ReferenceDocument,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            inventory.CurrentStock = newStock;
            inventory.ModifiedAt = DateTime.UtcNow;
            _uow.InventoryRepository.Update(inventory);

            await _uow.InventoryMovementRepository.AddAsync(movement);
            await _uow.CommitAsync();

            if (newStock <= inventory.MinimumStock)
                await TrySendLowStockAlertAsync(product.Name, inventory.MinimumStock, newStock).ConfigureAwait(false);

            return movement;
        }

        private async Task TrySendLowStockAlertAsync(string productName, decimal minimumStock, decimal currentStock)
        {
            try
            {
                var alertOn = await _settingService.GetSettingOrDefaultAsync(
                    SettingKeys.NotificationsAlertOnLowStock,
                    SettingKeys.NotificationsAlertOnLowStockDefault.ToString(CultureInfo.InvariantCulture));
                var recipients = await _settingService.GetSettingOrDefaultAsync(
                    SettingKeys.NotificationsLowStockRecipients,
                    SettingKeys.NotificationsLowStockRecipientsDefault);

                if (!IsTrue(alertOn) || string.IsNullOrWhiteSpace(recipients))
                    return;

                var emails = recipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => e.Length > 0)
                    .ToList();
                if (emails.Count == 0)
                    return;

                var subject = "Alerta: Stock bajo mínimo";
                var body = $"<p>El producto <strong>{WebUtility.HtmlEncode(productName)}</strong> ha quedado con stock bajo o igual al mínimo.</p>"
                    + $"<p>Stock actual: <strong>{currentStock}</strong></p>"
                    + $"<p>Stock mínimo: <strong>{minimumStock}</strong></p>"
                    + "<p>Considere reponer inventario.</p>";

                foreach (var email in emails)
                {
                    try
                    {
                        await _emailService.SendEmailResponseAsync(subject, body, email).ConfigureAwait(false);
                    }
                    catch
                    {
                        // No fallar el movimiento si falla el envío a un destinatario
                    }
                }
            }
            catch
            {
                // No propagar: el movimiento ya se guardó correctamente
            }
        }

        private static bool IsTrue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim().ToLowerInvariant();
            return v == "1" || v == "true" || v == "yes";
        }

        public async Task<InventoryMovement> GetMovement(int id)
        {
            var movement = await _uow.InventoryMovementRepository
                .GetAllIncluding(m => m.Product, m => m.Location)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movement == null)
                throw new InventoryMovementNotFoundException(_localizer);
            return movement;
        }

        public async Task<PaginatedList<InventoryMovement>> GetAllMovements(int? page, int? perPage, string sortOrder = null)
        {
            var movements = _uow.InventoryMovementRepository.GetAllIncluding(m => m.Product, m => m.Location);
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<InventoryMovement>.CreateAsync(movements, pageIndex, perPageIndex);
        }

        public async Task<PaginatedList<InventoryMovement>> GetMovementsByProduct(int productId, int locationId, int? page, int? perPage)
        {
            var movements = _uow.InventoryMovementRepository
                .GetAllIncluding(m => m.Product, m => m.Location)
                .Where(m => m.ProductId == productId && m.LocationId == locationId);
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<InventoryMovement>.CreateAsync(movements, pageIndex, perPageIndex);
        }
    }
}
