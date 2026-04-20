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
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace APICore.Tests.Unit.ProductLocationOffers
{
    public class ProductLocationOffersTests
    {
        private sealed class TestInventorySettings : IInventorySettings
        {
            public int RoundingDecimals => 4;
            public int PriceRoundingDecimals => 2;
            public bool AllowNegativeStock => false;
            public string DefaultUnitOfMeasure => "unit";
            public decimal DefaultMinimumStock => 0;
            public void InvalidateCache() { }
        }

        private static readonly TestInventorySettings InvSettings = new();

        private static CoreDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CoreDbContext>()
                .UseInMemoryDatabase($"OffersTests_{Guid.NewGuid()}")
                .Options;
            return new CoreDbContext(options);
        }

        private static void SeedOrgProducts(CoreDbContext ctx)
        {
            var now = DateTime.UtcNow;
            ctx.Organizations.Add(new Organization
            {
                Id = 1,
                Name = "Org",
                Code = "O1",
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.Locations.Add(new Location
            {
                Id = 1,
                OrganizationId = 1,
                Name = "Tienda A",
                Code = "A",
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.Locations.Add(new Location
            {
                Id = 2,
                OrganizationId = 1,
                Name = "Tienda B",
                Code = "B",
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.Products.Add(new Product
            {
                Id = 10,
                OrganizationId = 1,
                Code = "ELAB1",
                Name = "Elaborado 1",
                Description = "",
                Precio = 5,
                Costo = 1,
                ImagenUrl = "",
                IsAvailable = true,
                IsForSale = true,
                Tipo = ProductType.elaborado,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.Products.Add(new Product
            {
                Id = 11,
                OrganizationId = 1,
                Code = "INV1",
                Name = "Inventariable 1",
                Description = "",
                Precio = 3,
                Costo = 1,
                ImagenUrl = "",
                IsAvailable = true,
                IsForSale = true,
                Tipo = ProductType.inventariable,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.Inventories.Add(new Inventory
            {
                ProductId = 11,
                LocationId = 1,
                CurrentStock = 10,
                MinimumStock = 0,
                UnitOfMeasure = "u",
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task PublicCatalog_ByLocation_Excludes_Elaborado_Without_Offer()
        {
            await using var ctx = CreateContext();
            SeedOrgProducts(ctx);

            var catalog = new PublicCatalogService(ctx, InvSettings);
            var items = (await catalog.GetCatalogByLocationAsync(1)).ToList();
            Assert.DoesNotContain(items, i => i.Id == 10);
            Assert.Contains(items, i => i.Id == 11);
        }

        [Fact]
        public async Task PublicCatalog_ByLocation_Includes_Elaborado_With_Offer()
        {
            await using var ctx = CreateContext();
            SeedOrgProducts(ctx);
            var now = DateTime.UtcNow;
            ctx.ProductLocationOffers.Add(new ProductLocationOffer
            {
                ProductId = 10,
                LocationId = 1,
                OrganizationId = 1,
                CreatedAt = now,
                ModifiedAt = now,
            });
            await ctx.SaveChangesAsync();

            var catalog = new PublicCatalogService(ctx, InvSettings);
            var items = (await catalog.GetCatalogByLocationAsync(1)).ToList();
            Assert.Contains(items, i => i.Id == 10);
        }

        [Fact]
        public async Task CreateSaleOrder_Elaborado_Without_Offer_Throws()
        {
            await using var ctx = CreateContext();
            SeedOrgProducts(ctx);

            var uow = new UnitOfWork(ctx);
            var inv = new Mock<IInventorySettings>();
            inv.Setup(x => x.RoundingDecimals).Returns(2);
            inv.Setup(x => x.PriceRoundingDecimals).Returns(2);
            inv.Setup(x => x.AllowNegativeStock).Returns(false);
            inv.Setup(x => x.DefaultUnitOfMeasure).Returns("u");
            inv.Setup(x => x.DefaultMinimumStock).Returns(0m);

            var promo = new Mock<IPromotionService>();
            promo.Setup(x => x.GetActivePromotionForProduct(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .ReturnsAsync((Promotion)null);

            var loc = new Mock<IStringLocalizer<ISaleOrderService>>();
            loc.Setup(x => x[It.IsAny<string>()]).Returns((string s) => new LocalizedString(s, s));

            var metrics = new Mock<ICatalogMetricsTrackingService>();
            var loyalty = new Mock<ILoyaltyService>();

            var svc = new SaleOrderService(uow, ctx, loc.Object, inv.Object, promo.Object, metrics.Object, loyalty.Object);

            await Assert.ThrowsAsync<ProductNotOfferedAtLocationBadRequestException>(async () =>
                await svc.CreateSaleOrder(new CreateSaleOrderRequest
                {
                    LocationId = 1,
                    Items = new System.Collections.Generic.List<CreateSaleOrderItemRequest>
                    {
                        new CreateSaleOrderItemRequest { ProductId = 10, Quantity = 1m },
                    },
                }, userId: 0));
        }

        [Fact]
        public async Task CreateSaleOrder_Elaborado_With_Offer_Succeeds()
        {
            await using var ctx = CreateContext();
            SeedOrgProducts(ctx);
            var now = DateTime.UtcNow;
            ctx.ProductLocationOffers.Add(new ProductLocationOffer
            {
                ProductId = 10,
                LocationId = 1,
                OrganizationId = 1,
                CreatedAt = now,
                ModifiedAt = now,
            });
            await ctx.SaveChangesAsync();

            ctx.CurrentOrganizationId = 1;

            var uow = new UnitOfWork(ctx);
            var inv = new Mock<IInventorySettings>();
            inv.Setup(x => x.RoundingDecimals).Returns(2);
            inv.Setup(x => x.PriceRoundingDecimals).Returns(2);
            inv.Setup(x => x.AllowNegativeStock).Returns(false);
            inv.Setup(x => x.DefaultUnitOfMeasure).Returns("u");
            inv.Setup(x => x.DefaultMinimumStock).Returns(0m);

            var promo = new Mock<IPromotionService>();
            promo.Setup(x => x.GetActivePromotionForProduct(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .ReturnsAsync((Promotion)null);

            var loc = new Mock<IStringLocalizer<ISaleOrderService>>();
            loc.Setup(x => x[It.IsAny<string>()]).Returns((string s) => new LocalizedString(s, s));

            var metrics = new Mock<ICatalogMetricsTrackingService>();
            var loyalty = new Mock<ILoyaltyService>();

            var svc = new SaleOrderService(uow, ctx, loc.Object, inv.Object, promo.Object, metrics.Object, loyalty.Object);

            var order = await svc.CreateSaleOrder(new CreateSaleOrderRequest
            {
                LocationId = 1,
                Items = new System.Collections.Generic.List<CreateSaleOrderItemRequest>
                {
                    new CreateSaleOrderItemRequest { ProductId = 10, Quantity = 1m },
                },
            }, userId: 0);

            Assert.NotNull(order);
            Assert.Single(order.Items);
        }
    }
}
