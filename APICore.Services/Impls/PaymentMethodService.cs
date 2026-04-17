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
            var exists = await _uow.PaymentMethodRepository.FindAllAsync(pm => pm.OrganizationId == orgId && pm.Name == name);
            if (exists != null && exists.Count > 0)
                throw new PaymentMethodNameInUseBadRequestException(_localizer);

            var entity = new PaymentMethod
            {
                OrganizationId = orgId,
                Name = name,
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

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var name = request.Name.Trim();
                var orgId = _context.CurrentOrganizationId;
                var exists = await _uow.PaymentMethodRepository.FindAllAsync(pm =>
                    pm.OrganizationId == orgId && pm.Name == name && pm.Id != id);
                if (exists != null && exists.Count > 0)
                    throw new PaymentMethodNameInUseBadRequestException(_localizer);

                old.Name = name;
            }

            if (request.SortOrder.HasValue)
                old.SortOrder = request.SortOrder.Value;
            if (request.IsActive.HasValue)
                old.IsActive = request.IsActive.Value;

            _uow.PaymentMethodRepository.Update(old);
            await _uow.CommitAsync();
        }
    }
}
