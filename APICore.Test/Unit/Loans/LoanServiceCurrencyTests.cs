using APICore.Common.Constants;
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
using System.Threading.Tasks;
using Xunit;

namespace APICore.Tests.Unit.Loans
{
    public class LoanServiceCurrencyTests
    {
        private static CoreDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CoreDbContext>()
                .UseInMemoryDatabase($"LoanCurrency_{Guid.NewGuid()}")
                .Options;
            return new CoreDbContext(options);
        }

        private static (Currency cup, Currency usd) SeedOrgAndCurrencies(CoreDbContext ctx)
        {
            var now = DateTime.UtcNow;
            ctx.Organizations.Add(new Organization
            {
                Id = 1,
                Name = "Org",
                Code = "O1",
                IsVerified = false,
                CreatedAt = now,
                ModifiedAt = now,
            });
            var cup = new Currency
            {
                OrganizationId = 1,
                Code = "CUP",
                Name = "Peso cubano",
                ExchangeRate = 1m,
                IsActive = true,
                IsBase = true,
                CreatedAt = now,
                ModifiedAt = now,
            };
            ctx.Currencies.Add(cup);
            ctx.SaveChanges();
            var usd = new Currency
            {
                OrganizationId = 1,
                Code = "USD",
                Name = "Dólar",
                ExchangeRate = 120m,
                IsActive = true,
                IsBase = false,
                CreatedAt = now,
                ModifiedAt = now,
            };
            ctx.Currencies.Add(usd);
            ctx.SaveChanges();
            return (cup, usd);
        }

        private static LoanService CreateSut(
            CoreDbContext ctx,
            Mock<ICurrencyService>? currencyMock = null,
            Mock<ISettingService>? settingMock = null)
        {
            ctx.CurrentOrganizationId = 1;
            var uow = new UnitOfWork(ctx);
            var cur = currencyMock ?? new Mock<ICurrencyService>();
            cur.Setup(x => x.EnsureBaseCurrencyForOrganizationAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            var set = settingMock ?? new Mock<ISettingService>();
            if (settingMock == null)
            {
                set.Setup(x => x.GetSettingOrDefaultAsync(SettingKeys.DefaultDisplayCurrencyId, It.IsAny<string>()))
                    .ReturnsAsync("");
            }
            var loc = new Mock<IStringLocalizer<ILoanService>>();
            loc.Setup(x => x[It.IsAny<string>()]).Returns((string k) => new LocalizedString(k, k));
            return new LoanService(uow, ctx, cur.Object, set.Object, loc.Object);
        }

        [Fact]
        public async Task CreateLoan_WithoutPrincipalCurrencyId_UsesBaseWhenNoDefaultDisplaySetting()
        {
            await using var ctx = CreateContext();
            var (cup, _) = SeedOrgAndCurrencies(ctx);
            var sut = CreateSut(ctx);

            var result = await sut.CreateLoan(new CreateLoanRequest
            {
                DebtorName = "Juan",
                PrincipalAmount = 100m,
            });

            Assert.Equal(cup.Id, result.PrincipalCurrencyId);
            Assert.Equal("CUP", result.PrincipalCurrencyCode);
        }

        [Fact]
        public async Task CreateLoan_WithoutPrincipalCurrencyId_UsesDefaultDisplayWhenSettingPointsToActiveCurrency()
        {
            await using var ctx = CreateContext();
            var (_, usd) = SeedOrgAndCurrencies(ctx);
            var settingMock = new Mock<ISettingService>();
            settingMock.Setup(x => x.GetSettingOrDefaultAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(usd.Id.ToString());
            var sut = CreateSut(ctx, settingMock: settingMock);

            var result = await sut.CreateLoan(new CreateLoanRequest
            {
                DebtorName = "Juan",
                PrincipalAmount = 100m,
            });

            Assert.Equal("USD", result.PrincipalCurrencyCode);
            Assert.Equal(usd.Id, result.PrincipalCurrencyId);
        }

        [Fact]
        public async Task CreateLoan_WithExplicitPrincipalCurrencyId_SetsThatCurrency()
        {
            await using var ctx = CreateContext();
            var (_, usd) = SeedOrgAndCurrencies(ctx);
            var sut = CreateSut(ctx);

            var result = await sut.CreateLoan(new CreateLoanRequest
            {
                DebtorName = "Juan",
                PrincipalAmount = 50m,
                PrincipalCurrencyId = usd.Id,
            });

            Assert.Equal(usd.Id, result.PrincipalCurrencyId);
            Assert.Equal("USD", result.PrincipalCurrencyCode);
        }

        [Fact]
        public async Task CreateLoan_InactiveCurrency_Throws()
        {
            await using var ctx = CreateContext();
            SeedOrgAndCurrencies(ctx);
            var now = DateTime.UtcNow;
            var inactive = new Currency
            {
                OrganizationId = 1,
                Code = "EUR",
                Name = "Euro",
                ExchangeRate = 130m,
                IsActive = false,
                IsBase = false,
                CreatedAt = now,
                ModifiedAt = now,
            };
            ctx.Currencies.Add(inactive);
            await ctx.SaveChangesAsync();

            var sut = CreateSut(ctx);

            await Assert.ThrowsAsync<LoanPrincipalCurrencyInactiveBadRequestException>(() =>
                sut.CreateLoan(new CreateLoanRequest
                {
                    DebtorName = "Juan",
                    PrincipalAmount = 10m,
                    PrincipalCurrencyId = inactive.Id,
                }));
        }

        [Fact]
        public async Task UpdateLoan_ChangesPrincipalCurrencyId()
        {
            await using var ctx = CreateContext();
            var (cup, usd) = SeedOrgAndCurrencies(ctx);
            var sut = CreateSut(ctx);

            var created = await sut.CreateLoan(new CreateLoanRequest
            {
                DebtorName = "Juan",
                PrincipalAmount = 100m,
                PrincipalCurrencyId = cup.Id,
            });

            await sut.UpdateLoan(created.Id, new UpdateLoanRequest
            {
                PrincipalCurrencyId = usd.Id,
            });

            var after = await sut.GetLoan(created.Id);
            Assert.Equal(usd.Id, after.PrincipalCurrencyId);
            Assert.Equal("USD", after.PrincipalCurrencyCode);
        }
    }
}
