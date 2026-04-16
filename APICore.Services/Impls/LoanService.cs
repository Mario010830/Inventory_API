using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Common.Enums;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class LoanService : ILoanService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly ICurrencyService _currencyService;
        private readonly ISettingService _settingService;
        private readonly IStringLocalizer<ILoanService> _localizer;

        public LoanService(
            IUnitOfWork uow,
            CoreDbContext context,
            ICurrencyService currencyService,
            ISettingService settingService,
            IStringLocalizer<ILoanService> localizer)
        {
            _uow = uow;
            _context = context;
            _currencyService = currencyService;
            _settingService = settingService;
            _localizer = localizer;
        }

        public async Task<LoanResponse> CreateLoan(CreateLoanRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            if (request.PrincipalAmount <= 0)
                throw new LoanInvalidPrincipalBadRequestException(_localizer);

            if (request.InterestPercent is { } rate && rate < 0)
                throw new LoanInvalidPrincipalBadRequestException(_localizer);

            if (!LoanInterestRatePeriodParsing.TryParse(request.InterestRatePeriod, out var interestPeriod))
                throw new LoanInvalidPrincipalBadRequestException(_localizer);

            if (request.PrincipalCurrencyId.HasValue && request.PrincipalCurrencyId.Value <= 0)
                throw new LoanInvalidPrincipalBadRequestException(_localizer);

            int? principalCurrencyId;
            if (request.PrincipalCurrencyId is { } explicitCurrencyId && explicitCurrencyId > 0)
            {
                await RequireActivePrincipalCurrencyAsync(orgId, explicitCurrencyId);
                principalCurrencyId = explicitCurrencyId;
            }
            else
            {
                principalCurrencyId = await ResolveDefaultPrincipalCurrencyIdAsync(orgId);
            }

            var loan = new Loan
            {
                OrganizationId = orgId,
                DebtorName = request.DebtorName.Trim(),
                PrincipalAmount = request.PrincipalAmount,
                PrincipalCurrencyId = principalCurrencyId,
                Notes = request.Notes,
                InterestPercent = request.InterestPercent,
                InterestRatePeriod = interestPeriod,
                InterestStartDate = NormalizeDateOptional(request.InterestStartDate),
                DueDatesJson = SerializeDueDates(request.DueDates),
            };

            await _uow.LoanRepository.AddAsync(loan);
            await _uow.CommitAsync();

            return MapToResponse(await LoadLoan(loan.Id));
        }

        public async Task UpdateLoan(int id, UpdateLoanRequest request)
        {
            var loan = await LoadLoan(id);
            if (loan == null)
                throw new LoanNotFoundException(_localizer);

            var totalPaid = loan.Payments.Sum(p => p.Amount);

            if (request.PrincipalAmount is decimal np)
            {
                if (np <= 0 || np < totalPaid)
                    throw new LoanInvalidPrincipalBadRequestException(_localizer);
                loan.PrincipalAmount = np;
            }

            if (request.DebtorName != null)
                loan.DebtorName = request.DebtorName.Trim();
            if (request.Notes != null)
                loan.Notes = request.Notes;
            if (request.InterestPercent.HasValue)
            {
                if (request.InterestPercent.Value < 0)
                    throw new LoanInvalidPrincipalBadRequestException(_localizer);
                loan.InterestPercent = request.InterestPercent;
            }

            if (request.InterestRatePeriod != null)
            {
                if (!LoanInterestRatePeriodParsing.TryParse(request.InterestRatePeriod, out var interestPeriod))
                    throw new LoanInvalidPrincipalBadRequestException(_localizer);
                loan.InterestRatePeriod = interestPeriod;
            }

            if (request.PrincipalCurrencyId is int pcid && pcid > 0)
            {
                await RequireActivePrincipalCurrencyAsync(loan.OrganizationId, pcid);
                loan.PrincipalCurrencyId = pcid;
            }

            if (request.InterestStartDate.HasValue)
                loan.InterestStartDate = NormalizeDateOptional(request.InterestStartDate);

            if (request.DueDates != null)
                loan.DueDatesJson = SerializeDueDates(request.DueDates);

            await _uow.LoanRepository.UpdateAsync(loan, loan.Id);
            await _uow.CommitAsync();
        }

        public async Task DeleteLoan(int id)
        {
            var loan = await _uow.LoanRepository.FirstOrDefaultAsync(l => l.Id == id);
            if (loan == null)
                throw new LoanNotFoundException(_localizer);

            _uow.LoanRepository.Delete(loan);
            await _uow.CommitAsync();
        }

        public async Task<LoanResponse> GetLoan(int id)
        {
            var loan = await LoadLoan(id);
            if (loan == null)
                throw new LoanNotFoundException(_localizer);
            return MapToResponse(loan);
        }

        public async Task<PaginatedList<LoanResponse>> GetLoans(int? page, int? perPage)
        {
            var query = _uow.LoanRepository.GetAll()
                .Include(l => l.Payments)
                .Include(l => l.PrincipalCurrency)
                .OrderByDescending(l => l.CreatedAt);

            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            var paged = await PaginatedList<Loan>.CreateAsync(query, pageIndex, perPageIndex);
            var mapped = paged.Select(MapToResponse).ToList();
            return new PaginatedList<LoanResponse>(mapped, paged.TotalItems, paged.Page, paged.PerPage);
        }

        public async Task<LoanResponse> RegisterPayment(int loanId, RegisterLoanPaymentRequest request)
        {
            if (request.Amount <= 0)
                throw new LoanInvalidPaymentAmountBadRequestException(_localizer);

            var loan = await LoadLoan(loanId);
            if (loan == null)
                throw new LoanNotFoundException(_localizer);

            var payment = new LoanPayment
            {
                LoanId = loan.Id,
                Amount = request.Amount,
                PaidAt = NormalizeDateUtc(request.PaidAt),
                Notes = request.Notes,
            };

            await _uow.LoanPaymentRepository.AddAsync(payment);
            await _uow.CommitAsync();

            return MapToResponse(await LoadLoan(loanId));
        }

        private async Task<Loan?> LoadLoan(int id)
        {
            return await _uow.LoanRepository.GetAll()
                .Include(l => l.Payments)
                .Include(l => l.PrincipalCurrency)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        /// <summary>
        /// Moneda por defecto para el capital: moneda de visualización por defecto de la org si está activa;
        /// si no, la moneda base (CUP) de la organización.
        /// </summary>
        private async Task<int?> ResolveDefaultPrincipalCurrencyIdAsync(int organizationId)
        {
            await _currencyService.EnsureBaseCurrencyForOrganizationAsync(organizationId);

            var raw = await _settingService.GetSettingOrDefaultAsync(SettingKeys.DefaultDisplayCurrencyId, "");
            if (int.TryParse(raw, out var displayId) && displayId > 0)
            {
                var display = await _context.Currencies.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == displayId && c.OrganizationId == organizationId);
                if (display != null && display.IsActive)
                    return display.Id;
            }

            var baseCurrency = await _context.Currencies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.IsBase);
            return baseCurrency?.Id;
        }

        private async Task RequireActivePrincipalCurrencyAsync(int organizationId, int currencyId)
        {
            var c = await _context.Currencies
                .FirstOrDefaultAsync(x => x.Id == currencyId && x.OrganizationId == organizationId);
            if (c == null)
                throw new CurrencyNotFoundException();
            if (!c.IsActive)
                throw new LoanPrincipalCurrencyInactiveBadRequestException(_localizer);
        }

        private static DateTime? NormalizeDateOptional(DateTime? dt)
        {
            if (!dt.HasValue) return null;
            return dt.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt.Value.Date, DateTimeKind.Utc)
                : dt.Value.ToUniversalTime().Date;
        }

        private static DateTime NormalizeDateUtc(DateTime dt)
        {
            return dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                : dt.ToUniversalTime();
        }

        private static string? SerializeDueDates(IEnumerable<DateTime>? dates)
        {
            if (dates == null)
                return null;
            var list = dates.Select(d => d.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(d.Date, DateTimeKind.Utc)
                    : d.ToUniversalTime().Date)
                .Distinct()
                .OrderBy(d => d)
                .Select(d => d.ToString("yyyy-MM-dd"))
                .ToList();
            if (list.Count == 0)
                return null;
            return JsonSerializer.Serialize(list);
        }

        private static IReadOnlyList<DateTime> ParseDueDates(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<DateTime>();
            try
            {
                var strings = JsonSerializer.Deserialize<List<string>>(json);
                if (strings == null || strings.Count == 0)
                    return Array.Empty<DateTime>();
                var result = new List<DateTime>();
                foreach (var s in strings)
                {
                    if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                            out var d))
                        result.Add(d.Date);
                }
                return result.OrderBy(x => x).ToList();
            }
            catch
            {
                return Array.Empty<DateTime>();
            }
        }

        /// <summary>Interés simple sobre el saldo de capital actual, proporcional al tiempo desde InterestStartDate.</summary>
        private static decimal EstimateInterest(
            decimal outstandingPrincipal,
            decimal percent,
            LoanInterestRatePeriod period,
            DateTime interestStartUtc,
            DateTime asOfUtc)
        {
            if (outstandingPrincipal <= 0m || percent <= 0m)
                return 0m;
            var start = interestStartUtc.Date;
            var end = asOfUtc.Date;
            if (end < start)
                return 0m;
            var days = (decimal)(end - start).TotalDays;
            var r = percent / 100m;
            var factor = period switch
            {
                LoanInterestRatePeriod.daily => days,
                LoanInterestRatePeriod.weekly => days / 7m,
                LoanInterestRatePeriod.monthly => days / (365.25m / 12m),
                LoanInterestRatePeriod.annual => days / 365.25m,
                _ => 0m
            };
            return Math.Round(outstandingPrincipal * r * factor, 2, MidpointRounding.AwayFromZero);
        }

        private LoanResponse MapToResponse(Loan loan)
        {
            var totalPaid = loan.Payments.Sum(p => p.Amount);
            var outstanding = Math.Max(0m, loan.PrincipalAmount - totalPaid);
            var now = DateTime.UtcNow;
            decimal estimatedInterest = 0m;
            if (loan.InterestPercent is > 0m && loan.InterestStartDate.HasValue)
                estimatedInterest = EstimateInterest(outstanding, loan.InterestPercent.Value, loan.InterestRatePeriod, loan.InterestStartDate.Value, now);

            var payments = loan.Payments
                .OrderByDescending(p => p.PaidAt)
                .ThenByDescending(p => p.Id)
                .Select(p => new LoanPaymentResponse
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    PaidAt = p.PaidAt,
                    Notes = p.Notes,
                    CreatedAt = p.CreatedAt,
                })
                .ToList();

            return new LoanResponse
            {
                Id = loan.Id,
                DebtorName = loan.DebtorName,
                PrincipalAmount = loan.PrincipalAmount,
                PrincipalCurrencyId = loan.PrincipalCurrencyId,
                PrincipalCurrencyCode = loan.PrincipalCurrency?.Code,
                Notes = loan.Notes,
                InterestPercent = loan.InterestPercent,
                InterestRatePeriod = loan.InterestRatePeriod.ToString(),
                InterestStartDate = loan.InterestStartDate,
                DueDates = ParseDueDates(loan.DueDatesJson),
                TotalPaid = totalPaid,
                OutstandingPrincipal = outstanding,
                EstimatedInterest = estimatedInterest,
                EstimatedTotalDue = outstanding + estimatedInterest,
                Payments = payments,
                CreatedAt = loan.CreatedAt,
                ModifiedAt = loan.ModifiedAt,
            };
        }
    }
}
