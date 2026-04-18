using APICore.Common.DTO;
using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
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

namespace APICore.Tests.Unit.Sales
{
    public class CurrencyDenominationServiceTests
    {
        private static CoreDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CoreDbContext>()
                .UseInMemoryDatabase($"Denom_{Guid.NewGuid()}")
                .Options;
            return new CoreDbContext(options);
        }

        private static CurrencyDenominationService CreateSut(CoreDbContext ctx)
        {
            ctx.CurrentOrganizationId = 1;
            var uow = new UnitOfWork(ctx);
            var acc = new Mock<ICurrentUserContextAccessor>();
            acc.Setup(a => a.GetCurrent()).Returns((CurrentUserContext?)null);
            var loc = new Mock<IStringLocalizer<object>>();
            loc.Setup(x => x[It.IsAny<string>()]).Returns((string k) => new LocalizedString(k, k));
            return new CurrencyDenominationService(uow, ctx, acc.Object, loc.Object);
        }

        private static void SeedCurrency(CoreDbContext ctx, int currencyId = 1)
        {
            var now = DateTime.UtcNow;
            ctx.Organizations.Add(new Organization
            {
                Id = 1,
                Name = "Org",
                Code = "O1",
                IsVerified = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.Currencies.Add(new Currency
            {
                Id = currencyId,
                OrganizationId = 1,
                Code = "USD",
                Name = "Dólar",
                ExchangeRate = 120m,
                IsActive = true,
                IsBase = false,
                CreatedAt = now,
                ModifiedAt = now,
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task Create_ThenList_ReturnsDenomination()
        {
            await using var ctx = CreateContext();
            SeedCurrency(ctx);
            var sut = CreateSut(ctx);

            var created = await sut.CreateAsync(1, new CreateCurrencyDenominationRequest { Value = 50m, SortOrder = 1 });
            Assert.True(created.Id > 0);

            var list = await sut.GetByCurrencyAsync(1, activeOnly: false);
            Assert.Single(list);
            Assert.Equal(50m, list[0].Value);
        }

        [Fact]
        public async Task Create_DuplicateValue_Throws()
        {
            await using var ctx = CreateContext();
            SeedCurrency(ctx);
            var sut = CreateSut(ctx);

            await sut.CreateAsync(1, new CreateCurrencyDenominationRequest { Value = 10m, SortOrder = 0 });
            await Assert.ThrowsAsync<BaseBadRequestException>(() =>
                sut.CreateAsync(1, new CreateCurrencyDenominationRequest { Value = 10m, SortOrder = 1 }));
        }

        [Fact]
        public async Task Delete_RemovesRow()
        {
            await using var ctx = CreateContext();
            SeedCurrency(ctx);
            var sut = CreateSut(ctx);

            var created = await sut.CreateAsync(1, new CreateCurrencyDenominationRequest { Value = 5m, SortOrder = 0 });
            await sut.DeleteAsync(1, created.Id);

            Assert.False(await ctx.CurrencyDenominations.AnyAsync());
        }
    }
}
