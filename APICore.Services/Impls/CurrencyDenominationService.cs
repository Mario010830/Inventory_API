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
    public class CurrencyDenominationService : ICurrencyDenominationService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly ICurrentUserContextAccessor _currentUserContextAccessor;
        private readonly IStringLocalizer<object> _objectLocalizer;

        public CurrencyDenominationService(
            IUnitOfWork uow,
            CoreDbContext context,
            ICurrentUserContextAccessor currentUserContextAccessor,
            IStringLocalizer<object> objectLocalizer)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _currentUserContextAccessor = currentUserContextAccessor ?? throw new ArgumentNullException(nameof(currentUserContextAccessor));
            _objectLocalizer = objectLocalizer ?? throw new ArgumentNullException(nameof(objectLocalizer));
        }

        public async Task<IReadOnlyList<CurrencyDenominationResponse>> GetByCurrencyAsync(int currencyId, bool activeOnly = false)
        {
            await RequireCurrencyInOrgAsync(currencyId);

            var q = _context.CurrencyDenominations.AsNoTracking().Where(d => d.CurrencyId == currencyId);
            if (activeOnly)
                q = q.Where(d => d.IsActive);

            var list = await q.OrderBy(d => d.SortOrder).ThenByDescending(d => d.Value).ToListAsync();
            return list.Select(Map).ToList();
        }

        public async Task<CurrencyDenominationResponse> CreateAsync(int currencyId, CreateCurrencyDenominationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            await RequireCurrencyInOrgAsync(currencyId);

            var value = Math.Round(request.Value, 4, MidpointRounding.AwayFromZero);
            if (value <= 0)
                throw new BaseBadRequestException { CustomMessage = "El valor facial debe ser mayor que cero." };

            var exists = await _context.CurrencyDenominations.AnyAsync(d => d.CurrencyId == currencyId && d.Value == value);
            if (exists)
                throw new BaseBadRequestException { CustomMessage = "Ya existe una denominación con ese valor para esta moneda." };

            var entity = new CurrencyDenomination
            {
                CurrencyId = currencyId,
                Value = value,
                SortOrder = request.SortOrder,
                IsActive = true,
            };
            await _uow.CurrencyDenominationRepository.AddAsync(entity);
            await _uow.CommitAsync();

            return Map(entity);
        }

        public async Task<CurrencyDenominationResponse> UpdateAsync(int currencyId, int id, UpdateCurrencyDenominationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            await RequireCurrencyInOrgAsync(currencyId);

            var entity = await _context.CurrencyDenominations.FirstOrDefaultAsync(d => d.Id == id && d.CurrencyId == currencyId);
            if (entity == null)
                throw new CurrencyDenominationNotFoundException();

            if (request.Value.HasValue)
            {
                var value = Math.Round(request.Value.Value, 4, MidpointRounding.AwayFromZero);
                if (value <= 0)
                    throw new BaseBadRequestException { CustomMessage = "El valor facial debe ser mayor que cero." };

                var dup = await _context.CurrencyDenominations.AnyAsync(d => d.CurrencyId == currencyId && d.Value == value && d.Id != id);
                if (dup)
                    throw new BaseBadRequestException { CustomMessage = "Ya existe otra denominación con ese valor para esta moneda." };

                entity.Value = value;
            }

            if (request.SortOrder.HasValue)
                entity.SortOrder = request.SortOrder.Value;

            if (request.IsActive.HasValue)
                entity.IsActive = request.IsActive.Value;

            _uow.CurrencyDenominationRepository.Update(entity);
            await _uow.CommitAsync();

            return Map(entity);
        }

        public async Task DeleteAsync(int currencyId, int id)
        {
            await RequireCurrencyInOrgAsync(currencyId);

            var entity = await _context.CurrencyDenominations.FirstOrDefaultAsync(d => d.Id == id && d.CurrencyId == currencyId);
            if (entity == null)
                throw new CurrencyDenominationNotFoundException();

            _uow.CurrencyDenominationRepository.Delete(entity);
            await _uow.CommitAsync();
        }

        private async Task RequireCurrencyInOrgAsync(int currencyId)
        {
            var orgId = await RequireOrganizationIdAsync();
            var ok = await _context.Currencies.AnyAsync(c => c.Id == currencyId && c.OrganizationId == orgId);
            if (!ok)
                throw new CurrencyNotFoundException();
        }

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

            throw new OrganizationContextRequiredException(_objectLocalizer);
        }

        private static CurrencyDenominationResponse Map(CurrencyDenomination d) =>
            new()
            {
                Id = d.Id,
                CurrencyId = d.CurrencyId,
                Value = d.Value,
                SortOrder = d.SortOrder,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                ModifiedAt = d.ModifiedAt,
            };
    }
}
