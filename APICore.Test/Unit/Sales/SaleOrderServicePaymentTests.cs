using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services;
using APICore.Services.Exceptions;
using APICore.Services.Impls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace APICore.Tests.Unit.Sales
{
    public class SaleOrderServicePaymentTests
    {
        private static CoreDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CoreDbContext>()
                .UseInMemoryDatabase($"SalePay_{Guid.NewGuid()}")
                .Options;
            return new CoreDbContext(options);
        }

        private sealed class TestInventorySettings : IInventorySettings
        {
            public int RoundingDecimals => 2;
            public int PriceRoundingDecimals => 2;
            public bool AllowNegativeStock => true;
            public string DefaultUnitOfMeasure => "unit";
            public decimal DefaultMinimumStock => 0;
            public void InvalidateCache() { }
        }

        private static (CoreDbContext ctx, int locationId, int productId, int pmCashId, int pmTransferId) SeedSaleScenario(CoreDbContext ctx)
        {
            var now = DateTime.UtcNow;
            ctx.IgnoreLocationFilter = true;

            ctx.Organizations.Add(new Organization
            {
                Id = 1,
                Name = "Org",
                Code = "O1",
                IsVerified = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.Locations.Add(new Location
            {
                Id = 1,
                OrganizationId = 1,
                Name = "Loc",
                Code = "L1",
                IsVerified = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.Products.Add(new Product
            {
                Id = 1,
                OrganizationId = 1,
                Code = "P1",
                Name = "Prod",
                Description = "",
                Precio = 100m,
                Costo = 50m,
                ImagenUrl = "",
                IsAvailable = true,
                IsForSale = true,
                Tipo = ProductType.inventariable,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.Inventories.Add(new Inventory
            {
                ProductId = 1,
                LocationId = 1,
                CurrentStock = 50m,
                MinimumStock = 0,
                UnitOfMeasure = "unit",
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.PaymentMethods.Add(new PaymentMethod
            {
                Id = 1,
                OrganizationId = 1,
                Name = "Efectivo",
                SortOrder = 0,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.PaymentMethods.Add(new PaymentMethod
            {
                Id = 2,
                OrganizationId = 1,
                Name = "Transferencia",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.SaveChanges();
            return (ctx, 1, 1, 1, 2);
        }

        /// <summary>CUP (90) y moneda FX (91) con tasa 2 CUP por 1 FX; denominaciones 20 y 10.</summary>
        private static (int cupId, int fxId) SeedFxCurrency(CoreDbContext ctx)
        {
            var now = DateTime.UtcNow;
            ctx.Currencies.Add(new Currency
            {
                Id = 90,
                OrganizationId = 1,
                Code = "CUP",
                Name = "CUP",
                ExchangeRate = 1m,
                IsActive = true,
                IsBase = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.Currencies.Add(new Currency
            {
                Id = 91,
                OrganizationId = 1,
                Code = "FX",
                Name = "FX",
                ExchangeRate = 2m,
                IsActive = true,
                IsBase = false,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.CurrencyDenominations.Add(new CurrencyDenomination
            {
                CurrencyId = 91,
                Value = 20m,
                SortOrder = 0,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.CurrencyDenominations.Add(new CurrencyDenomination
            {
                CurrencyId = 91,
                Value = 10m,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.SaveChanges();
            return (90, 91);
        }

        private static SaleOrderService CreateSut(CoreDbContext ctx)
        {
            ctx.CurrentOrganizationId = -1;
            ctx.CurrentLocationId = -1;
            var uow = new UnitOfWork(ctx);
            var loc = new Mock<IStringLocalizer<ISaleOrderService>>();
            loc.Setup(x => x[It.IsAny<string>()]).Returns((string k) => new LocalizedString(k, k));
            var promo = new Mock<IPromotionService>();
            promo.Setup(x => x.GetActivePromotionForProduct(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .ReturnsAsync(default(Promotion));
            var metrics = new Mock<ICatalogMetricsTrackingService>();
            metrics.Setup(x => x.StagePurchaseCompletedEvents(It.IsAny<SaleOrder>()));
            return new SaleOrderService(uow, ctx, loc.Object, new TestInventorySettings(), promo.Object, metrics.Object);
        }

        [Fact]
        public async Task CreateSaleOrder_WithSplitPayments_PersistsLines()
        {
            await using var ctx = CreateContext();
            var (_, locId, prodId, pmCash, pmTransfer) = SeedSaleScenario(ctx);
            var sut = CreateSut(ctx);

            var order = await sut.CreateSaleOrder(new CreateSaleOrderRequest
            {
                LocationId = locId,
                Items = new List<CreateSaleOrderItemRequest>
                {
                    new() { ProductId = prodId, Quantity = 1m },
                },
                Payments = new List<CreateSaleOrderPaymentRequest>
                {
                    new() { PaymentMethodId = pmCash, Amount = 40m },
                    new() { PaymentMethodId = pmTransfer, Amount = 60m },
                },
            }, userId: 0);

            Assert.Equal(100m, order.Total);
            Assert.Equal(2, order.Payments.Count);
            Assert.Equal(100m, order.Payments.Sum(p => p.Amount));
        }

        [Fact]
        public async Task CreateSaleOrder_WhenPaymentSumDoesNotMatchTotal_Throws()
        {
            await using var ctx = CreateContext();
            var (_, locId, prodId, pmCash, pmTransfer) = SeedSaleScenario(ctx);
            var sut = CreateSut(ctx);

            await Assert.ThrowsAsync<SaleOrderPaymentsMismatchTotalBadRequestException>(() =>
                sut.CreateSaleOrder(new CreateSaleOrderRequest
                {
                    LocationId = locId,
                    Items = new List<CreateSaleOrderItemRequest>
                    {
                        new() { ProductId = prodId, Quantity = 1m },
                    },
                    Payments = new List<CreateSaleOrderPaymentRequest>
                    {
                        new() { PaymentMethodId = pmCash, Amount = 40m },
                        new() { PaymentMethodId = pmTransfer, Amount = 50m },
                    },
                }, userId: 0));
        }

        [Fact]
        public async Task ConfirmSaleOrder_WithoutPayments_Throws()
        {
            await using var ctx = CreateContext();
            var (_, locId, prodId, _, _) = SeedSaleScenario(ctx);
            var sut = CreateSut(ctx);

            var order = await sut.CreateSaleOrder(new CreateSaleOrderRequest
            {
                LocationId = locId,
                Items = new List<CreateSaleOrderItemRequest>
                {
                    new() { ProductId = prodId, Quantity = 1m },
                },
            }, userId: 0);

            await Assert.ThrowsAsync<SaleOrderPaymentsRequiredBadRequestException>(() =>
                sut.ConfirmSaleOrder(order.Id, userId: 1));
        }

        [Fact]
        public async Task ConfirmSaleOrder_WithMatchingPayments_Confirms()
        {
            await using var ctx = CreateContext();
            var (_, locId, prodId, pmCash, pmTransfer) = SeedSaleScenario(ctx);
            var sut = CreateSut(ctx);

            var order = await sut.CreateSaleOrder(new CreateSaleOrderRequest
            {
                LocationId = locId,
                Items = new List<CreateSaleOrderItemRequest>
                {
                    new() { ProductId = prodId, Quantity = 1m },
                },
                Payments = new List<CreateSaleOrderPaymentRequest>
                {
                    new() { PaymentMethodId = pmCash, Amount = 25m },
                    new() { PaymentMethodId = pmTransfer, Amount = 75m },
                },
            }, userId: 0);

            var confirmed = await sut.ConfirmSaleOrder(order.Id, userId: 1);
            Assert.Equal(SaleOrderStatus.confirmed, confirmed.Status);
        }

        [Fact]
        public async Task CreateSaleOrder_WithForeignCurrency_PersistsSnapshot()
        {
            await using var ctx = CreateContext();
            var (_, locId, prodId, pmCash, _) = SeedSaleScenario(ctx);
            var (_, fxId) = SeedFxCurrency(ctx);
            var sut = CreateSut(ctx);

            var order = await sut.CreateSaleOrder(new CreateSaleOrderRequest
            {
                LocationId = locId,
                Items = new List<CreateSaleOrderItemRequest>
                {
                    new() { ProductId = prodId, Quantity = 1m },
                },
                Payments = new List<CreateSaleOrderPaymentRequest>
                {
                    new()
                    {
                        PaymentMethodId = pmCash,
                        Amount = 100m,
                        CurrencyId = fxId,
                        AmountForeign = 50m,
                        ExchangeRateSnapshot = 2m,
                    },
                },
            }, userId: 0);

            var pay = Assert.Single(order.Payments);
            Assert.Equal(fxId, pay.CurrencyId);
            Assert.Equal(50m, pay.AmountForeign);
            Assert.Equal(2m, pay.ExchangeRateSnapshot);
            Assert.Equal(100m, pay.Amount);
        }

        [Fact]
        public async Task CreateSaleOrder_WithTenderAndChange_PersistsDenominations()
        {
            await using var ctx = CreateContext();
            var (_, locId, prodId, pmCash, _) = SeedSaleScenario(ctx);
            var (_, fxId) = SeedFxCurrency(ctx);
            var sut = CreateSut(ctx);

            var order = await sut.CreateSaleOrder(new CreateSaleOrderRequest
            {
                LocationId = locId,
                Items = new List<CreateSaleOrderItemRequest>
                {
                    new() { ProductId = prodId, Quantity = 1m },
                },
                Payments = new List<CreateSaleOrderPaymentRequest>
                {
                    new()
                    {
                        PaymentMethodId = pmCash,
                        Amount = 100m,
                        CurrencyId = fxId,
                        AmountForeign = 50m,
                        ExchangeRateSnapshot = 2m,
                        TenderDenominations = new List<SaleOrderPaymentDenominationLineRequest>
                        {
                            new() { Value = 20m, Quantity = 3 },
                            new() { Value = 10m, Quantity = 2 },
                        },
                        ChangeDenominations = new List<SaleOrderPaymentDenominationLineRequest>
                        {
                            new() { Value = 10m, Quantity = 3 },
                        },
                    },
                },
            }, userId: 0);

            var pay = Assert.Single(order.Payments);
            Assert.Equal(3, pay.Denominations.Count);
            Assert.Equal(8, pay.Denominations.Sum(d => d.Quantity));
        }

        [Fact]
        public async Task CreateSaleOrder_WhenExchangeRateMismatch_Throws()
        {
            await using var ctx = CreateContext();
            var (_, locId, prodId, pmCash, _) = SeedSaleScenario(ctx);
            var (_, fxId) = SeedFxCurrency(ctx);
            var sut = CreateSut(ctx);

            await Assert.ThrowsAsync<BaseBadRequestException>(() =>
                sut.CreateSaleOrder(new CreateSaleOrderRequest
                {
                    LocationId = locId,
                    Items = new List<CreateSaleOrderItemRequest>
                    {
                        new() { ProductId = prodId, Quantity = 1m },
                    },
                    Payments = new List<CreateSaleOrderPaymentRequest>
                    {
                        new()
                        {
                            PaymentMethodId = pmCash,
                            Amount = 100m,
                            CurrencyId = fxId,
                            AmountForeign = 50m,
                            ExchangeRateSnapshot = 3m,
                        },
                    },
                }, userId: 0));
        }
    }
}
