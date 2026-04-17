using APICore.Common.DTO.Request;
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
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<IPaymentMethodService> _localizer;

        public PaymentMethodService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<IPaymentMethodService> localizer)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
        }

        public async Task<PaymentMethod> CreateAsync(CreatePaymentMethodRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var name = request.Name.Trim();
            var instrRef = NormalizeInstrumentReference(request.InstrumentReference);
            if (await PaymentMethodNameAndRefExistsAsync(orgId, name, instrRef, excludeId: null))
                throw new PaymentMethodNameInUseBadRequestException(_localizer);

            var entity = new PaymentMethod
            {
                OrganizationId = orgId,
                Name = name,
                InstrumentReference = instrRef,
                SortOrder = request.SortOrder,
                IsActive = true,
            };
            await _uow.PaymentMethodRepository.AddAsync(entity);
            await _uow.CommitAsync();
            return entity;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _uow.PaymentMethodRepository.FirstOrDefaultAsync(pm => pm.Id == id);
            if (entity == null)
                throw new PaymentMethodNotFoundException(_localizer);

            var used = await _context.SaleOrderPayments.IgnoreQueryFilters()
                .AnyAsync(p => p.PaymentMethodId == id);
            if (used)
                throw new PaymentMethodInSalesBadRequestException(_localizer);

            _uow.PaymentMethodRepository.Delete(entity);
            await _uow.CommitAsync();
        }

        public async Task<PaginatedList<PaymentMethod>> GetAllAsync(int? page, int? perPage)
        {
            var query = _uow.PaymentMethodRepository.GetAll().OrderBy(pm => pm.SortOrder).ThenBy(pm => pm.Name);
            return await PaginatedList<PaymentMethod>.CreateAsync(query, page ?? 1, perPage ?? 10);
        }

        public async Task<IReadOnlyList<PaymentMethod>> GetActiveByLocationIdAsync(int locationId)
        {
            var location = await _context.Locations.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == locationId);
            if (location == null)
                throw new LocationNotFoundException(_localizer);

            return await _context.PaymentMethods.IgnoreQueryFilters()
                .Where(pm => pm.OrganizationId == location.OrganizationId && pm.IsActive)
                .OrderBy(pm => pm.SortOrder)
                .ThenBy(pm => pm.Name)
                .ToListAsync();
        }

        public async Task<PaymentMethod> GetAsync(int id)
        {
            var entity = await _uow.PaymentMethodRepository.FirstOrDefaultAsync(pm => pm.Id == id);
            if (entity == null)
                throw new PaymentMethodNotFoundException(_localizer);
            return entity;
        }

        public async Task UpdateAsync(int id, UpdatePaymentMethodRequest request)
        {
            var old = await _uow.PaymentMethodRepository.FirstOrDefaultAsync(pm => pm.Id == id);
            if (old == null)
                throw new PaymentMethodNotFoundException(_localizer);

            var orgId = _context.CurrentOrganizationId;

            var newName = !string.IsNullOrWhiteSpace(request.Name) ? request.Name.Trim() : old.Name;
            var newInstr = request.InstrumentReference != null
                ? NormalizeInstrumentReference(request.InstrumentReference)
                : NormalizeInstrumentReference(old.InstrumentReference);

            if (await PaymentMethodNameAndRefExistsAsync(orgId, newName, newInstr, excludeId: id))
                throw new PaymentMethodNameInUseBadRequestException(_localizer);

            if (!string.IsNullOrWhiteSpace(request.Name))
                old.Name = newName;
            if (request.InstrumentReference != null)
                old.InstrumentReference = newInstr;

            if (request.SortOrder.HasValue)
                old.SortOrder = request.SortOrder.Value;
            if (request.IsActive.HasValue)
                old.IsActive = request.IsActive.Value;

            _uow.PaymentMethodRepository.Update(old);
            await _uow.CommitAsync();
        }

        public async Task EnsureCashPaymentMethodExistsAsync(int organizationId)
        {
            if (organizationId <= 0)
                return;

            const string cashName = "Efectivo";
            var hasPlainCash = await _context.PaymentMethods.IgnoreQueryFilters()
                .AnyAsync(pm => pm.OrganizationId == organizationId
                    && pm.Name == cashName
                    && pm.InstrumentReference == null);

            if (hasPlainCash)
                return;

            var now = DateTime.UtcNow;
            await _uow.PaymentMethodRepository.AddAsync(new PaymentMethod
            {
                OrganizationId = organizationId,
                Name = cashName,
                InstrumentReference = null,
                SortOrder = 0,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
            await _uow.CommitAsync();
        }

        private static string? NormalizeInstrumentReference(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var t = value.Trim();
            return t.Length > 120 ? t[..120] : t;
        }

        private async Task<bool> PaymentMethodNameAndRefExistsAsync(int orgId, string name, string? instrumentRef, int? excludeId)
        {
            var list = await _uow.PaymentMethodRepository.FindAllAsync(pm =>
                pm.OrganizationId == orgId &&
                pm.Name == name &&
                (excludeId == null || pm.Id != excludeId.Value));

            if (list == null || list.Count == 0)
                return false;

            foreach (var pm in list)
            {
                if (NormalizeInstrumentReference(pm.InstrumentReference) == instrumentRef)
                    return true;
            }

            return false;
        }
    }
}
