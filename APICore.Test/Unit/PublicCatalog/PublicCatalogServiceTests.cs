#nullable enable
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Services.Impls;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace APICore.Tests.Unit.PublicCatalog
{
    public class PublicCatalogServiceTests
    {
        private static CoreDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CoreDbContext>()
                .UseInMemoryDatabase($"PublicCatalogTests_{Guid.NewGuid()}")
                .Options;
            return new CoreDbContext(options);
        }

        private static readonly DateTime Now = DateTime.UtcNow;

        private static void SeedBase(CoreDbContext ctx)
        {
            ctx.Organizations.Add(new Organization
            {
                Id = 1, Name = "Org Test", Code = "OT",
                CreatedAt = Now, ModifiedAt = Now,
            });

            ctx.BusinessCategories.Add(new BusinessCategory
            {
                Id = 1, Name = "Restaurante", Icon = "restaurant",
                Slug = "restaurante", IsActive = true, SortOrder = 1,
                CreatedAt = Now, ModifiedAt = Now,
            });

            ctx.BusinessCategories.Add(new BusinessCategory
            {
                Id = 2, Name = "Ferretería", Icon = "hardware",
                Slug = "ferreteria", IsActive = true, SortOrder = 2,
                CreatedAt = Now, ModifiedAt = Now,
            });

            ctx.Locations.Add(new Location
            {
                Id = 1, OrganizationId = 1, Name = "Tienda A", Code = "A",
                IsVerified = true, OffersDelivery = true, OffersPickup = false,
                Latitude = 22.38, Longitude = -80.15,
                Province = "Cienfuegos", Municipality = "Centro",
                BusinessCategoryId = 1,
                BusinessHoursJson = JsonSerializer.Serialize(new
                {
                    monday = new { open = "08:00", close = "20:00" },
                    tuesday = new { open = "08:00", close = "20:00" },
                    wednesday = new { open = "08:00", close = "20:00" },
                    thursday = new { open = "08:00", close = "20:00" },
                    friday = new { open = "08:00", close = "20:00" },
                    saturday = new { open = "09:00", close = "14:00" },
                }),
                CreatedAt = Now.AddDays(-10), ModifiedAt = Now,
            });

            ctx.Locations.Add(new Location
            {
                Id = 2, OrganizationId = 1, Name = "Tienda B", Code = "B",
                IsVerified = false, OffersDelivery = false, OffersPickup = true,
                Latitude = 22.42, Longitude = -79.96,
                Province = "Cienfuegos", Municipality = "Palmira",
                BusinessCategoryId = 2,
                CreatedAt = Now.AddDays(-5), ModifiedAt = Now,
            });

            ctx.Locations.Add(new Location
            {
                Id = 3, OrganizationId = 1, Name = "Tienda C", Code = "C",
                IsVerified = false, OffersDelivery = true, OffersPickup = true,
                Province = "Habana", Municipality = "Vedado",
                CreatedAt = Now.AddDays(-1), ModifiedAt = Now,
            });

            // Products
            ctx.Products.Add(new Product
            {
                Id = 1, OrganizationId = 1, Name = "Producto 1", Code = "P1",
                Precio = 1500, IsForSale = true, Tipo = ProductType.inventariable,
                CreatedAt = Now, ModifiedAt = Now,
            });
            ctx.Products.Add(new Product
            {
                Id = 2, OrganizationId = 1, Name = "Producto 2", Code = "P2",
                Precio = 3000, IsForSale = true, Tipo = ProductType.inventariable,
                CreatedAt = Now, ModifiedAt = Now,
            });
            ctx.Products.Add(new Product
            {
                Id = 3, OrganizationId = 1, Name = "Producto 3", Code = "P3",
                Precio = 500, IsForSale = true, Tipo = ProductType.inventariable,
                CreatedAt = Now, ModifiedAt = Now,
            });

            // Inventories: Tienda A has 3 products, Tienda B has 1
            ctx.Inventories.Add(new Inventory { Id = 1, LocationId = 1, ProductId = 1, CurrentStock = 10, CreatedAt = Now, ModifiedAt = Now });
            ctx.Inventories.Add(new Inventory { Id = 2, LocationId = 1, ProductId = 2, CurrentStock = 5, CreatedAt = Now, ModifiedAt = Now });
            ctx.Inventories.Add(new Inventory { Id = 3, LocationId = 1, ProductId = 3, CurrentStock = 0, CreatedAt = Now, ModifiedAt = Now });
            ctx.Inventories.Add(new Inventory { Id = 4, LocationId = 2, ProductId = 1, CurrentStock = 20, CreatedAt = Now, ModifiedAt = Now });

            // Active promotion on Product 1
            ctx.Promotions.Add(new Promotion
            {
                Id = 1, OrganizationId = 1, ProductId = 1,
                Type = PromotionType.percentage, Value = 10, IsActive = true, MinQuantity = 1,
                CreatedAt = Now, ModifiedAt = Now,
            });

            ctx.SaveChanges();
        }

        // =====================================================================
        // GetLocationsAsync — new fields
        // =====================================================================

        [Fact]
        public async Task GetLocations_ReturnsNewFields()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = (await svc.GetLocationsAsync()).ToList();

            var a = result.First(l => l.Id == 1);
            Assert.True(a.IsVerified);
            Assert.Equal(a.IsOpenNow && true, a.OffersDelivery);
            Assert.Equal(a.IsOpenNow && false, a.OffersPickup);
            Assert.True(a.CreatedAt < DateTime.UtcNow);

            var b = result.First(l => l.Id == 2);
            Assert.False(b.IsVerified);
            Assert.Equal(b.IsOpenNow && false, b.OffersDelivery);
            Assert.Equal(b.IsOpenNow && true, b.OffersPickup);
        }

        [Fact]
        public async Task GetLocations_ProductCount_Correct()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = (await svc.GetLocationsAsync()).ToList();

            Assert.Equal(3, result.First(l => l.Id == 1).ProductCount); // 3 inventories
            Assert.Equal(1, result.First(l => l.Id == 2).ProductCount); // 1 inventory
            Assert.Equal(0, result.First(l => l.Id == 3).ProductCount); // no inventory
        }

        [Fact]
        public async Task GetLocations_HasPromo_True_WhenLocationHasPromoProduct()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = (await svc.GetLocationsAsync()).ToList();

            // Tienda A & B both have Product 1 which has an active promotion
            Assert.True(result.First(l => l.Id == 1).HasPromo);
            Assert.True(result.First(l => l.Id == 2).HasPromo);
            Assert.False(result.First(l => l.Id == 3).HasPromo);
        }

        // =====================================================================
        // GetLocationsAsync — sorting
        // =====================================================================

        [Fact]
        public async Task GetLocations_SortByProductCount_Descending()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = (await svc.GetLocationsAsync(sortBy: "productCount")).ToList();

            Assert.Equal(1, result[0].Id); // 3 products
            Assert.Equal(2, result[1].Id); // 1 product
            Assert.Equal(3, result[2].Id); // 0 products
        }

        [Fact]
        public async Task GetLocations_SortByNewest_Descending()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = (await svc.GetLocationsAsync(sortBy: "newest")).ToList();

            Assert.Equal(3, result[0].Id); // created 1 day ago
            Assert.Equal(2, result[1].Id); // created 5 days ago
            Assert.Equal(1, result[2].Id); // created 10 days ago
        }

        [Fact]
        public async Task GetLocations_SortByName_Ascending()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = (await svc.GetLocationsAsync(sortBy: "name", sortDir: "asc")).ToList();

            Assert.Equal("Tienda A", result[0].Name);
            Assert.Equal("Tienda B", result[1].Name);
            Assert.Equal("Tienda C", result[2].Name);
        }

        // =====================================================================
        // GetLocationsAsync — filtering
        // =====================================================================

        [Fact]
        public async Task GetLocations_FilterByCategoryId()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = (await svc.GetLocationsAsync(categoryId: 1)).ToList();

            Assert.Single(result);
            Assert.Equal("Tienda A", result[0].Name);
        }

        [Fact]
        public async Task GetLocations_FilterByRadius_NarrowRadius()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            // Tienda A is at (22.38, -80.15), search near it with 5km radius
            var result = (await svc.GetLocationsAsync(lat: 22.38, lng: -80.15, radiusKm: 5)).ToList();

            Assert.Single(result);
            Assert.Equal("Tienda A", result[0].Name);
        }

        [Fact]
        public async Task GetLocations_FilterByRadius_WideRadius()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            // Wide radius should include A and B (both have coords)
            var result = (await svc.GetLocationsAsync(lat: 22.40, lng: -80.05, radiusKm: 50)).ToList();

            Assert.Equal(2, result.Count); // C has no coordinates, excluded
        }

        [Fact]
        public async Task GetLocations_FilterByRadius_ExcludesNoCoords()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            // Even with huge radius, Tienda C has no coords → excluded
            var result = (await svc.GetLocationsAsync(lat: 22.40, lng: -80.05, radiusKm: 1000)).ToList();

            Assert.DoesNotContain(result, l => l.Name == "Tienda C");
        }

        // =====================================================================
        // GetCatalogAllAsync — sorting
        // =====================================================================

        [Fact]
        public async Task GetCatalogAll_SortByPriceAsc()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = await svc.GetCatalogAllAsync(1, 50, sortBy: "price", sortDir: "asc");

            var prices = result.Items.Select(i => i.Precio).ToList();
            for (int i = 1; i < prices.Count; i++)
                Assert.True(prices[i] >= prices[i - 1], $"Price {prices[i]} should be >= {prices[i - 1]}");
        }

        [Fact]
        public async Task GetCatalogAll_SortByPriceDesc()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = await svc.GetCatalogAllAsync(1, 50, sortBy: "price", sortDir: "desc");

            var prices = result.Items.Select(i => i.Precio).ToList();
            for (int i = 1; i < prices.Count; i++)
                Assert.True(prices[i] <= prices[i - 1], $"Price {prices[i]} should be <= {prices[i - 1]}");
        }

        [Fact]
        public async Task GetCatalogAll_SortByPromo_PromoFirst()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = await svc.GetCatalogAllAsync(1, 50, sortBy: "promo");
            var items = result.Items.ToList();

            // First items should have promotion
            var promoItems = items.TakeWhile(i => i.HasActivePromotion).ToList();
            Assert.True(promoItems.Count > 0, "Should have promo items first");
        }

        // =====================================================================
        // GetCatalogAllAsync — filtering
        // =====================================================================

        [Fact]
        public async Task GetCatalogAll_FilterByMinMaxPrice()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = await svc.GetCatalogAllAsync(1, 50, minPrice: 1000, maxPrice: 2000);

            Assert.All(result.Items, item =>
            {
                Assert.True(item.Precio >= 1000);
                Assert.True(item.Precio <= 2000);
            });
        }

        [Fact]
        public async Task GetCatalogAll_FilterByInStock()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = await svc.GetCatalogAllAsync(1, 50, inStock: true);

            Assert.All(result.Items, item =>
            {
                Assert.True(item.Tipo == "elaborado" || item.StockAtLocation > 0,
                    $"Product {item.Name} has stock={item.StockAtLocation} tipo={item.Tipo}");
            });
        }

        [Fact]
        public async Task GetCatalogAll_FilterByHasPromotion()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var result = await svc.GetCatalogAllAsync(1, 50, hasPromotion: true);

            Assert.All(result.Items, item => Assert.True(item.HasActivePromotion));
        }

        // =====================================================================
        // GetCatalogAllAsync — pagination
        // =====================================================================

        [Fact]
        public async Task GetCatalogAll_Pagination_ReturnsCorrectPage()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var page1 = await svc.GetCatalogAllAsync(1, 2);
            var page2 = await svc.GetCatalogAllAsync(2, 2);

            Assert.Equal(2, page1.Items.Count());
            Assert.True(page1.TotalPages > 1);
            Assert.Equal(1, page1.Page);
            Assert.Equal(2, page2.Page);

            // Pages should not overlap
            var ids1 = page1.Items.Select(i => $"{i.Id}-{i.LocationId}").ToHashSet();
            var ids2 = page2.Items.Select(i => $"{i.Id}-{i.LocationId}");
            Assert.Empty(ids1.Intersect(ids2));
        }

        [Fact]
        public async Task GetCatalogAll_FilterReducesTotal()
        {
            using var ctx = CreateContext();
            SeedBase(ctx);
            var svc = new PublicCatalogService(ctx);

            var all = await svc.GetCatalogAllAsync(1, 100);
            var filtered = await svc.GetCatalogAllAsync(1, 100, minPrice: 2000);

            Assert.True(filtered.Total <= all.Total);
        }
    }
}
