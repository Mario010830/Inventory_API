using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly ISettingService _settingService;
        private readonly ICurrentUserContextAccessor _currentUserContextAccessor;
        private readonly IStringLocalizer<ICurrencyService> _localizer;

        public CurrencyService(
            IUnitOfWork uow,
            CoreDbContext context,
            ISettingService settingService,
            ICurrentUserContextAccessor currentUserContextAccessor,
            IStringLocalizer<ICurrencyService> localizer)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));
            _currentUserContextAccessor = currentUserContextAccessor ?? throw new ArgumentNullException(nameof(currentUserContextAccessor));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task EnsureBaseCurrencyForOrganizationAsync(int organizationId)
        {
            if (organizationId <= 0)
                return;

            var hasBase = await _context.Currencies
                .IgnoreQueryFilters()
                .AnyAsync(c => c.OrganizationId == organizationId && c.IsBase);
            if (hasBase)
                return;

            var now = DateTime.UtcNow;
            var cup = new Currency
            {
                OrganizationId = organizationId,
                Code = "CUP",
                Name = "Peso cubano",
                ExchangeRate = 1m,
                IsActive = true,
                IsBase = true,
                CreatedAt = now,
                ModifiedAt = now
            };
            await _uow.CurrencyRepository.AddAsync(cup);
            await _uow.CommitAsync();

            var hasDefaultSetting = await _context.Setting
                .IgnoreQueryFilters()
                .AnyAsync(s => s.OrganizationId == organizationId && s.Key == SettingKeys.DefaultDisplayCurrencyId);
            if (!hasDefaultSetting)
            {
                await _uow.SettingRepository.AddAsync(new Setting
                {
                    OrganizationId = organizationId,
                    Key = SettingKeys.DefaultDisplayCurrencyId,
                    Value = cup.Id.ToString()
                });
                await _uow.CommitAsync();
            }
        }

        public async Task<IEnumerable<CurrencyResponse>> GetAllAsync()
        {
            var orgId = await RequireOrganizationIdAsync();
            await EnsureBaseCurrencyForOrganizationAsync(orgId);

            var defaultId = await GetDefaultDisplayCurrencyIdAsync();
            var list = await _uow.CurrencyRepository.GetAll()
                .AsNoTracking()
                .OrderBy(c => c.Code)
                .ToListAsync();
            return list.Select(c => Map(c, defaultId)).ToList();
        }

        public async Task<CurrencyResponse> GetByIdAsync(int id)
        {
            var orgId = await RequireOrganizationIdAsync();
            await EnsureBaseCurrencyForOrganizationAsync(orgId);

            var currency = await _uow.CurrencyRepository.FirstOrDefaultAsync(c => c.Id == id);
            if (currency == null)
                throw new CurrencyNotFoundException();

            var defaultId = await GetDefaultDisplayCurrencyIdAsync();
            return Map(currency, defaultId);
        }

        public async Task<CurrencyResponse> CreateAsync(CreateCurrencyRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var orgId = await RequireOrganizationIdAsync();
            await EnsureBaseCurrencyForOrganizationAsync(orgId);

            var code = NormalizeCode(request.Code);
            if (string.IsNullOrEmpty(code))
                throw new BaseBadRequestException { CustomCode = 400455, CustomMessage = "El código de moneda es obligatorio." };

            var name = (request.Name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
                throw new BaseBadRequestException { CustomCode = 400456, CustomMessage = "El nombre de moneda es obligatorio." };

            if (request.ExchangeRate <= 0)
                throw new BaseBadRequestException { CustomCode = 400457, CustomMessage = "El tipo de cambio debe ser mayor que cero." };

            var duplicate = await _uow.CurrencyRepository.FirstOrDefaultAsync(c => c.Code == code);
            if (duplicate != null)
                throw new CurrencyCodeInUseBadRequestException();

            var currency = new Currency
            {
                OrganizationId = orgId,
                Code = code,
                Name = name,
                ExchangeRate = request.ExchangeRate,
                IsActive = request.IsActive,
                IsBase = false
            };

            await _uow.CurrencyRepository.AddAsync(currency);
            await _uow.CommitAsync();

            var defaultId = await GetDefaultDisplayCurrencyIdAsync();
            return Map(currency, defaultId);
        }

        public async Task<CurrencyResponse> UpdateAsync(int id, UpdateCurrencyRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var orgId = await RequireOrganizationIdAsync();
            await EnsureBaseCurrencyForOrganizationAsync(orgId);

            var currency = await _uow.CurrencyRepository.GetAsync(id);
            if (currency == null)
                throw new CurrencyNotFoundException();

            if (currency.IsBase)
                throw new BaseCurrencyCannotModifyBadRequestException();

            if (request.Name != null)
            {
                var name = request.Name.Trim();
                if (string.IsNullOrEmpty(name))
                    throw new BaseBadRequestException { CustomCode = 400456, CustomMessage = "El nombre de moneda es obligatorio." };
                currency.Name = name;
            }

            if (request.ExchangeRate.HasValue)
            {
                if (request.ExchangeRate.Value <= 0)
                    throw new BaseBadRequestException { CustomCode = 400457, CustomMessage = "El tipo de cambio debe ser mayor que cero." };
                currency.ExchangeRate = request.ExchangeRate.Value;
            }

            if (request.IsActive.HasValue)
                currency.IsActive = request.IsActive.Value;

            _uow.CurrencyRepository.Update(currency);
            await _uow.CommitAsync();

            var defaultId = await GetDefaultDisplayCurrencyIdAsync();
            return Map(currency, defaultId);
        }

        public async Task DeleteAsync(int id)
        {
            var orgId = await RequireOrganizationIdAsync();
            await EnsureBaseCurrencyForOrganizationAsync(orgId);

            var currency = await _uow.CurrencyRepository.GetAsync(id);
            if (currency == null)
                throw new CurrencyNotFoundException();

            if (currency.IsBase)
                throw new BaseCurrencyCannotDeleteBadRequestException();

            var defaultId = await GetDefaultDisplayCurrencyIdAsync();
            if (defaultId.HasValue && defaultId.Value == id)
                throw new DefaultCurrencyCannotDeleteBadRequestException();

            _uow.CurrencyRepository.Delete(currency);
            await _uow.CommitAsync();
        }

        public async Task<CurrencyResponse> SetDefaultDisplayCurrencyAsync(SetDefaultCurrencyRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var orgId = await RequireOrganizationIdAsync();
            await EnsureBaseCurrencyForOrganizationAsync(orgId);

            var currency = await _uow.CurrencyRepository.GetAsync(request.CurrencyId);
            if (currency == null)
                throw new CurrencyNotFoundException();

            if (!currency.IsActive)
                throw new InvalidDefaultDisplayCurrencyBadRequestException(
                    "Solo se puede establecer como predeterminada una moneda activa.");

            await _settingService.SetSettingAsync(new SettingRequest
            {
                Key = SettingKeys.DefaultDisplayCurrencyId,
                Value = currency.Id.ToString()
            });

            var defaultId = await GetDefaultDisplayCurrencyIdAsync();
            return Map(currency, defaultId);
        }

        /// <summary>
        /// Usuarios con organización en el token usan ese Id. SuperAdmin sin organización usa la org DEFAULT del seed (o la primera por Id)
        /// para que el filtro multi-tenant y los settings funcionen igual que en el resto de módulos.
        /// </summary>
        private async Task<int> RequireOrganizationIdAsync()
        {
            if (_context.CurrentOrganizationId > 0)
                return _context.CurrentOrganizationId;

            var user = _currentUserContextAccessor.GetCurrent();
            if (user?.IsSuperAdmin == true)
            {
                var resolved = await _context.Organizations.IgnoreQueryFilters()
                    .Where(o => o.Code == "DEFAULT")
                    .Select(o => o.Id)
                    .FirstOrDefaultAsync();
                if (resolved == 0)
                {
                    resolved = await _context.Organizations.IgnoreQueryFilters()
                        .OrderBy(o => o.Id)
                        .Select(o => o.Id)
                        .FirstOrDefaultAsync();
                }

                if (resolved > 0)
                {
                    _context.CurrentOrganizationId = resolved;
                    return resolved;
                }
            }

            throw new OrganizationContextRequiredException(_localizer);
        }

        private async Task<int?> GetDefaultDisplayCurrencyIdAsync()
        {
            var raw = await _settingService.GetSettingOrDefaultAsync(SettingKeys.DefaultDisplayCurrencyId, "");
            if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out var id))
                return null;
            return id;
        }

        private static CurrencyResponse Map(Currency c, int? defaultDisplayId)
        {
            return new CurrencyResponse
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                ExchangeRate = c.ExchangeRate,
                IsActive = c.IsActive,
                IsBase = c.IsBase,
                IsDefaultDisplay = defaultDisplayId.HasValue && defaultDisplayId.Value == c.Id,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.ModifiedAt
            };
        }

        private static string NormalizeCode(string? code)
        {
            return (code ?? "").Trim().ToUpperInvariant();
        }
    }
}
