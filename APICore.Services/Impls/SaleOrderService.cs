using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class SaleOrderService : ISaleOrderService
    {
        private const decimal PaymentTotalTolerance = 0.01m;
        private const decimal ExchangeRateEqualityTolerance = 0.000001m;
        private const decimal CashDenominationTotalTolerance = 0.02m;

        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<ISaleOrderService> _localizer;
        private readonly IInventorySettings _inventorySettings;
        private readonly IPromotionService _promotionService;
        private readonly ICatalogMetricsTrackingService _catalogMetricsTrackingService;
        private readonly ILoyaltyService _loyaltyService;

        public SaleOrderService(
            IUnitOfWork uow,
            CoreDbContext context,
            IStringLocalizer<ISaleOrderService> localizer,
            IInventorySettings inventorySettings,
            IPromotionService promotionService,
            ICatalogMetricsTrackingService catalogMetricsTrackingService,
            ILoyaltyService loyaltyService)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
            _inventorySettings = inventorySettings;
            _promotionService = promotionService;
            _catalogMetricsTrackingService = catalogMetricsTrackingService ?? throw new ArgumentNullException(nameof(catalogMetricsTrackingService));
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
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
                if (contact.OrganizationId != orgId)
                    throw new ContactNotFoundException(_localizer);
                if (!contact.IsCustomer)
                    throw new ContactNotCustomerForSaleBadRequestException(_localizer);
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
                    .FirstOrDefaultAsync(p => p.Id == itemReq.ProductId && p.OrganizationId == orgId && !p.IsDeleted);
                if (product == null)
                    throw new ProductNotFoundException(_localizer);

                if (product.Tipo == ProductType.elaborado)
                {
                    var offered = await _context.ProductLocationOffers.IgnoreQueryFilters()
                        .AnyAsync(o => o.ProductId == product.Id && o.LocationId == request.LocationId);
                    if (!offered)
                        throw new ProductNotOfferedAtLocationBadRequestException(_localizer);
                }

                var qty = DecimalRoundingHelper.RoundQuantity(itemReq.Quantity, decimals);
                var originalUnitPrice = Math.Round(itemReq.UnitPrice ?? product.Precio, priceDecimals);
                var promotion = await _promotionService.GetActivePromotionForProduct(itemReq.ProductId, qty, orgId);
                var unitPrice = originalUnitPrice;
                if (promotion != null)
                {
                    unitPrice = promotion.Type == PromotionType.percentage
                        ? Math.Round(Math.Max(0, originalUnitPrice - (originalUnitPrice * (promotion.Value / 100m))), priceDecimals)
                        : Math.Round(Math.Max(0, promotion.Value), priceDecimals);
                }
                var unitCost = Math.Round(product.Costo, priceDecimals);
                var discount = Math.Round(itemReq.Discount, priceDecimals);
                var lineTotal = Math.Round(qty * unitPrice - discount, priceDecimals);

                order.Items.Add(new SaleOrderItem
                {
                    ProductId = itemReq.ProductId,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    OriginalUnitPrice = originalUnitPrice,
                    UnitCost = unitCost,
                    PromotionId = promotion?.Id,
                    Discount = discount,
                    LineTotal = lineTotal,
                });

                subtotal += lineTotal;
            }

            order.Subtotal = Math.Round(subtotal, priceDecimals);
            order.Total = Math.Round(order.Subtotal - order.DiscountAmount, priceDecimals);
            order.Folio = await GenerateFolioAsync(orgId);

            if (request.Payments != null && request.Payments.Count > 0)
            {
                await ValidatePaymentLinesAsync(orgId, order.Total, request.Payments, priceDecimals);
                foreach (var pay in request.Payments)
                    order.Payments.Add(CreatePaymentFromRequest(pay, priceDecimals));
            }

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

            if (order.Status == SaleOrderStatus.returned)
                throw new SaleOrderFullyReturnedBadRequestException(_localizer);

            if (order.Status == SaleOrderStatus.confirmed)
                return order;

            if (order.Payments == null || !order.Payments.Any())
                throw new SaleOrderPaymentsRequiredBadRequestException(_localizer);

            var paymentSum = order.Payments.Sum(p => p.Amount);
            if (!PaymentTotalsMatch(order.Total, paymentSum))
                throw new SaleOrderPaymentsMismatchTotalBadRequestException(_localizer);

            var allowNegative = _inventorySettings.AllowNegativeStock;
            var decimals = _inventorySettings.RoundingDecimals;
            var priceDecimals = _inventorySettings.PriceRoundingDecimals;

            foreach (var item in order.Items)
            {
                if (item.Product?.Tipo == ProductType.elaborado)
                {
                    var offered = await _context.ProductLocationOffers.IgnoreQueryFilters()
                        .AnyAsync(o => o.ProductId == item.ProductId && o.LocationId == order.LocationId);
                    if (!offered)
                        throw new ProductNotOfferedAtLocationBadRequestException(_localizer);
                    continue;
                }

                var (stockProductId, stockQty) = ProductStockResolution.GetDeductionUnits(item.Product!, item.Quantity, decimals);

                var inventory = await _uow.InventoryRepository.FirstOrDefaultAsync(
                    i => i.ProductId == stockProductId && i.LocationId == order.LocationId);

                if (inventory == null && !allowNegative)
                    throw new InsufficientStockBadRequestException(_localizer);

                if (inventory != null && !allowNegative && inventory.CurrentStock < stockQty)
                    throw new InsufficientStockBadRequestException(_localizer);
            }

            // Descontar inventario y crear movimientos
            foreach (var item in order.Items)
            {
                if (item.Product?.Tipo == ProductType.elaborado)
                    continue;

                var (stockProductId, stockQty) = ProductStockResolution.GetDeductionUnits(item.Product!, item.Quantity, decimals);

                var inventory = await _uow.InventoryRepository.FirstOrDefaultAsync(
                    i => i.ProductId == stockProductId && i.LocationId == order.LocationId);

                decimal previousStock = inventory?.CurrentStock ?? 0;
                decimal newStock = DecimalRoundingHelper.RoundQuantity(previousStock - stockQty, decimals);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = stockProductId,
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

                var movementUnitCost = item.UnitCost;
                if (ProductStockResolution.UsesParentStock(item.Product!) && item.Product!.StockParentProduct != null)
                    movementUnitCost = Math.Round(item.Product.StockParentProduct.Costo, priceDecimals);

                var movement = new InventoryMovement
                {
                    ProductId = stockProductId,
                    LocationId = order.LocationId,
                    Type = InventoryMovementType.exit,
                    Reason = InventoryMovementReason.Venta.ToString(),
                    Quantity = stockQty,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    UnitCost = movementUnitCost,
                    UnitPrice = item.UnitPrice,
                    SaleOrderId = order.Id,
                    ReferenceDocument = order.Folio,
                    UserId = userId,
                };
                await _uow.InventoryMovementRepository.AddAsync(movement);
            }

            order.Status = SaleOrderStatus.confirmed;
            _uow.SaleOrderRepository.Update(order);
            _catalogMetricsTrackingService.StagePurchaseCompletedEvents(order);
            await _loyaltyService.ProcessConfirmedSaleOrderAsync(order);
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

            if (order.Status == SaleOrderStatus.returned)
                throw new SaleOrderFullyReturnedBadRequestException(_localizer);

            // Si ya estaba confirmada, hay que revertir el inventario
            if (order.Status == SaleOrderStatus.confirmed)
            {
                var decimals = _inventorySettings.RoundingDecimals;
                var priceDecimals = _inventorySettings.PriceRoundingDecimals;

                foreach (var item in order.Items)
                {
                    if (item.Product?.Tipo == ProductType.elaborado)
                        continue;

                    var (stockProductId, stockQty) = ProductStockResolution.GetDeductionUnits(item.Product!, item.Quantity, decimals);

                    var inventory = await _uow.InventoryRepository.FirstOrDefaultAsync(
                        i => i.ProductId == stockProductId && i.LocationId == order.LocationId);

                    decimal previousStock = inventory?.CurrentStock ?? 0;
                    decimal newStock = DecimalRoundingHelper.RoundQuantity(previousStock + stockQty, decimals);

                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            ProductId = stockProductId,
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

                    var movementUnitCost = item.UnitCost;
                    if (ProductStockResolution.UsesParentStock(item.Product!) && item.Product!.StockParentProduct != null)
                        movementUnitCost = Math.Round(item.Product.StockParentProduct.Costo, priceDecimals);

                    // Movimiento de reversión
                    var reversal = new InventoryMovement
                    {
                        ProductId = stockProductId,
                        LocationId = order.LocationId,
                        Type = InventoryMovementType.entry,
                        Reason = InventoryMovementReason.Correccion.ToString(),
                        Quantity = stockQty,
                        PreviousStock = previousStock,
                        NewStock = newStock,
                        UnitCost = movementUnitCost,
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
            var order = await LoadFullOrder(id);
            if (order == null)
                throw new SaleOrderNotFoundException(_localizer);

            if (order.Status != SaleOrderStatus.draft)
                throw new SaleOrderCannotEditBadRequestException(_localizer);

            if (request.ContactId.HasValue)
            {
                var contact = await _uow.ContactRepository.FirstOrDefaultAsync(c => c.Id == request.ContactId.Value);
                if (contact == null)
                    throw new ContactNotFoundException(_localizer);
                if (contact.OrganizationId != order.OrganizationId)
                    throw new ContactNotFoundException(_localizer);
                if (!contact.IsCustomer)
                    throw new ContactNotCustomerForSaleBadRequestException(_localizer);
                order.ContactId = request.ContactId.Value;
            }

            if (request.Notes != null) order.Notes = request.Notes;
            if (request.DiscountAmount.HasValue)
            {
                order.DiscountAmount = Math.Round(request.DiscountAmount.Value, _inventorySettings.PriceRoundingDecimals);
                order.Total = Math.Round(order.Subtotal - order.DiscountAmount, _inventorySettings.PriceRoundingDecimals);
            }

            if (request.Payments != null)
            {
                var priceDecimals = _inventorySettings.PriceRoundingDecimals;
                foreach (var p in order.Payments.ToList())
                    _context.SaleOrderPayments.Remove(p);
                order.Payments.Clear();

                if (request.Payments.Count > 0)
                {
                    await ValidatePaymentLinesAsync(order.OrganizationId, order.Total, request.Payments, priceDecimals);
                    foreach (var pay in request.Payments)
                        order.Payments.Add(CreatePaymentFromRequest(pay, priceDecimals));
                }
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
            IQueryable<SaleOrder> query = _context.SaleOrders
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .Include(s => s.Payments).ThenInclude(p => p.PaymentMethod)
                .Include(s => s.Payments).ThenInclude(p => p.Currency)
                .Include(s => s.Payments).ThenInclude(p => p.Denominations)
                .Include(s => s.Contact)
                .Include(s => s.Location);

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

            var (cubaTodayStartUtc, cubaTodayEndExclusiveUtc) = CubaBusinessCalendar.GetCubaCalendarDayRangeUtc(DateTime.UtcNow);
            var revenue = orders.Sum(o => o.Total);
            var cogs = orders.SelectMany(o => o.Items).Sum(i => i.UnitCost * i.Quantity);
            var grossMargin = revenue - cogs;

            return new SaleStatsResponse
            {
                TotalSales = orders.Count,
                SalesToday = orders.Count(o => o.CreatedAt >= cubaTodayStartUtc && o.CreatedAt < cubaTodayEndExclusiveUtc),
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
                        .ThenInclude(p => p!.StockParentProduct)
                .Include(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .Include(s => s.Payments)
                    .ThenInclude(p => p.Currency)
                .Include(s => s.Payments)
                    .ThenInclude(p => p.Denominations)
                .Include(s => s.Contact)
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        private static bool PaymentTotalsMatch(decimal orderTotal, decimal paymentsSum) =>
            Math.Abs(orderTotal - paymentsSum) <= PaymentTotalTolerance;

        private static string? NormalizePaymentReference(string? reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return null;
            var t = reference.Trim();
            return t.Length > 120 ? t[..120] : t;
        }

        private async Task ValidatePaymentLinesAsync(int organizationId, decimal orderTotal, List<CreateSaleOrderPaymentRequest> lines, int priceDecimals)
        {
            if (lines == null || lines.Count == 0)
                return;

            var methodIds = lines.Select(l => l.PaymentMethodId).Distinct().ToList();
            var methods = await _context.PaymentMethods.IgnoreQueryFilters()
                .Where(pm => methodIds.Contains(pm.Id) && pm.OrganizationId == organizationId)
                .ToListAsync();

            if (methods.Count != methodIds.Count)
                throw new PaymentMethodNotFoundException(_localizer);

            foreach (var pm in methods)
            {
                if (!pm.IsActive)
                    throw new PaymentMethodInactiveBadRequestException(_localizer);
            }

            var currencyIds = lines
                .Where(l => l.CurrencyId is > 0)
                .Select(l => l.CurrencyId!.Value)
                .Distinct()
                .ToList();

            List<Currency> currencies = new();
            if (currencyIds.Count > 0)
            {
                currencies = await _context.Currencies.IgnoreQueryFilters()
                    .Where(c => currencyIds.Contains(c.Id) && c.OrganizationId == organizationId)
                    .ToListAsync();
                if (currencies.Count != currencyIds.Count)
                    throw new CurrencyNotFoundException();
            }

            foreach (var pay in lines)
                await ValidateSinglePaymentLineAsync(pay, currencies, priceDecimals);

            var sum = lines.Sum(l => Math.Round(l.Amount, priceDecimals));
            if (!PaymentTotalsMatch(orderTotal, sum))
                throw new SaleOrderPaymentsMismatchTotalBadRequestException(_localizer);
        }

        private async Task ValidateSinglePaymentLineAsync(CreateSaleOrderPaymentRequest pay, List<Currency> currencies, int priceDecimals)
        {
            if (pay.CurrencyId is null or <= 0)
            {
                if (pay.AmountForeign.HasValue || pay.ExchangeRateSnapshot.HasValue)
                    throw new BaseBadRequestException { CustomMessage = "No envíe amountForeign ni exchangeRateSnapshot si no indica currencyId." };
                if (HasDenominationLines(pay))
                    throw new BaseBadRequestException { CustomMessage = "Las denominaciones requieren currencyId y amountForeign." };
                return;
            }

            var currency = currencies.FirstOrDefault(c => c.Id == pay.CurrencyId!.Value);
            if (currency == null)
                throw new CurrencyNotFoundException();

            if (!currency.IsActive)
                throw new BaseBadRequestException { CustomMessage = "La moneda indicada no está activa." };

            if (!pay.AmountForeign.HasValue || pay.AmountForeign.Value <= 0)
                throw new BaseBadRequestException { CustomMessage = "amountForeign es obligatorio cuando se indica currencyId." };

            if (!pay.ExchangeRateSnapshot.HasValue)
                throw new BaseBadRequestException { CustomMessage = "exchangeRateSnapshot es obligatorio cuando se indica currencyId." };

            var dbRate = currency.IsBase ? 1m : currency.ExchangeRate;
            if (Math.Abs(pay.ExchangeRateSnapshot.Value - dbRate) > ExchangeRateEqualityTolerance)
                throw new BaseBadRequestException { CustomMessage = "La tasa de cambio no coincide con la configuración actual." };

            pay.ExchangeRateSnapshot = dbRate;

            var foreignRounded = Math.Round(pay.AmountForeign.Value, 4, MidpointRounding.AwayFromZero);
            pay.AmountForeign = foreignRounded;

            var expectedBase = Math.Round(foreignRounded * dbRate, priceDecimals);
            if (!PaymentTotalsMatch(pay.Amount, expectedBase))
                throw new BaseBadRequestException { CustomMessage = "El importe en CUP (amount) no coincide con amountForeign y la tasa vigente." };

            if (HasDenominationLines(pay))
                await ValidateCashDenominationsAsync(pay.CurrencyId.Value, pay, foreignRounded);
        }

        private static bool HasDenominationLines(CreateSaleOrderPaymentRequest pay)
        {
            static bool AnyQty(IEnumerable<SaleOrderPaymentDenominationLineRequest>? xs) =>
                xs != null && xs.Any(d => d.Quantity > 0);

            return AnyQty(pay.TenderDenominations) || AnyQty(pay.ChangeDenominations);
        }

        private async Task ValidateCashDenominationsAsync(int currencyId, CreateSaleOrderPaymentRequest pay, decimal amountForeignRounded)
        {
            decimal Sum(IEnumerable<SaleOrderPaymentDenominationLineRequest>? xs)
            {
                if (xs == null)
                    return 0m;
                decimal s = 0m;
                foreach (var d in xs)
                {
                    if (d.Quantity < 0)
                        throw new BaseBadRequestException { CustomMessage = "La cantidad de billetes no puede ser negativa." };
                    if (d.Quantity == 0)
                        continue;
                    if (d.Value <= 0)
                        throw new BaseBadRequestException { CustomMessage = "Cada denominación debe tener un valor mayor que cero." };
                    var v = Math.Round(d.Value, 4, MidpointRounding.AwayFromZero);
                    s += v * d.Quantity;
                }

                return Math.Round(s, 4, MidpointRounding.AwayFromZero);
            }

            var sumTender = Sum(pay.TenderDenominations);
            var sumChange = Sum(pay.ChangeDenominations);
            var netFromBills = Math.Round(sumTender - sumChange, 4, MidpointRounding.AwayFromZero);
            if (Math.Abs(netFromBills - amountForeignRounded) > CashDenominationTotalTolerance)
                throw new BaseBadRequestException { CustomMessage = "La suma de entregado menos vuelto no coincide con amountForeign." };

            var valuesNeeded = new HashSet<decimal>();
            void Collect(IEnumerable<SaleOrderPaymentDenominationLineRequest>? xs)
            {
                if (xs == null) return;
                foreach (var d in xs.Where(x => x.Quantity > 0))
                    valuesNeeded.Add(Math.Round(d.Value, 4, MidpointRounding.AwayFromZero));
            }

            Collect(pay.TenderDenominations);
            Collect(pay.ChangeDenominations);
            if (valuesNeeded.Count == 0)
                return;

            var allowed = await _context.CurrencyDenominations.IgnoreQueryFilters()
                .Where(cd => cd.CurrencyId == currencyId && cd.IsActive && valuesNeeded.Contains(cd.Value))
                .Select(cd => cd.Value)
                .ToListAsync();

            foreach (var v in valuesNeeded)
            {
                if (!allowed.Any(a => a == v))
                    throw new BaseBadRequestException { CustomMessage = $"El valor facial {v} no está configurado como denominación activa para esta moneda." };
            }
        }

        private static SaleOrderPayment CreatePaymentFromRequest(CreateSaleOrderPaymentRequest pay, int priceDecimals)
        {
            var entity = new SaleOrderPayment
            {
                PaymentMethodId = pay.PaymentMethodId,
                Amount = Math.Round(pay.Amount, priceDecimals),
                Reference = NormalizePaymentReference(pay.Reference),
                CurrencyId = pay.CurrencyId is > 0 ? pay.CurrencyId : null,
                AmountForeign = pay.CurrencyId is > 0 ? pay.AmountForeign : null,
                ExchangeRateSnapshot = pay.CurrencyId is > 0 ? pay.ExchangeRateSnapshot : null,
            };

            AddDenominationEntities(entity, pay.TenderDenominations, SaleOrderPaymentDenominationKind.tender);
            AddDenominationEntities(entity, pay.ChangeDenominations, SaleOrderPaymentDenominationKind.change);
            return entity;
        }

        private static void AddDenominationEntities(
            SaleOrderPayment entity,
            List<SaleOrderPaymentDenominationLineRequest>? list,
            SaleOrderPaymentDenominationKind kind)
        {
            if (list == null)
                return;
            foreach (var row in list)
            {
                if (row.Quantity <= 0)
                    continue;
                entity.Denominations.Add(new SaleOrderPaymentDenomination
                {
                    Kind = kind,
                    Value = Math.Round(row.Value, 4, MidpointRounding.AwayFromZero),
                    Quantity = row.Quantity,
                });
            }
        }

        private async Task<string> GenerateFolioAsync(int orgId)
        {
            // Debe ignorar filtros de tenant/ubicación: al crear en contexto anónimo o con
            // ubicación distinta, el query filter de SaleOrder ocultaría el resto y el
            // contador quedaría siempre en 0 → folios duplicados (p. ej. VENTA-0001).
            var count = await _context.SaleOrders.IgnoreQueryFilters()
                .Where(s => s.OrganizationId == orgId)
                .CountAsync();
            return $"VENTA-{(count + 1):D4}";
        }
    }
}
