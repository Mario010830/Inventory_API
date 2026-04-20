using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<ISupplierService> _localizer;

        public SupplierService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<ISupplierService> localizer)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
        }

        public async Task<Contact> CreateSupplier(CreateSupplierRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var nameExists = await _uow.ContactRepository.FindAllAsync(c =>
                c.Name == request.Name && c.OrganizationId == orgId && c.IsSupplier);
            if (nameExists != null && nameExists.Count > 0)
                throw new SupplierNameInUseBadRequestException(_localizer);

            var contact = new Contact
            {
                OrganizationId = orgId,
                Name = request.Name,
                Company = null,
                ContactPerson = request.ContactPerson,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                Notes = request.Notes,
                Origin = null,
                IsActive = request.IsActive,
                AssignedUserId = null,
                IsCustomer = false,
                IsSupplier = true,
                LeadStatus = null,
                LeadConvertedAt = null,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            await _uow.ContactRepository.AddAsync(contact);
            await _uow.CommitAsync();

            return contact;
        }

        public async Task DeleteSupplier(int id)
        {
            var contact = await _uow.ContactRepository.FirstOrDefaultAsync(c => c.Id == id && c.IsSupplier);
            if (contact == null)
                throw new SupplierNotFoundException(_localizer);

            var movements = await _uow.InventoryMovementRepository.FindAllAsync(m => m.SupplierContactId == id);
            if (movements != null && movements.Count > 0)
                throw new SupplierHasMovementsBadRequestException(_localizer);

            if (contact.IsCustomer)
            {
                contact.IsSupplier = false;
                contact.ModifiedAt = DateTime.UtcNow;
                await _uow.ContactRepository.UpdateAsync(contact, contact.Id);
            }
            else
                _uow.ContactRepository.Delete(contact);

            await _uow.CommitAsync();
        }

        public async Task<PaginatedList<Contact>> GetAllSuppliers(int? page, int? perPage, string sortOrder = null)
        {
            var suppliers = _uow.ContactRepository.GetAll().Where(c => c.IsSupplier);
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<Contact>.CreateAsync(suppliers, pageIndex, perPageIndex);
        }

        public async Task<Contact> GetSupplier(int id)
        {
            var contact = await _uow.ContactRepository.FirstOrDefaultAsync(c => c.Id == id && c.IsSupplier);
            if (contact == null)
                throw new SupplierNotFoundException(_localizer);
            return contact;
        }

        public async Task UpdateSupplier(int id, UpdateSupplierRequest request)
        {
            var old = await _uow.ContactRepository.FirstOrDefaultAsync(c => c.Id == id && c.IsSupplier);
            if (old == null)
                throw new SupplierNotFoundException(_localizer);

            if (request.Name != null)
            {
                var orgId = _context.CurrentOrganizationId;
                var nameExists = await _uow.ContactRepository.FindAllAsync(c =>
                    c.Name == request.Name && c.Id != id && c.OrganizationId == orgId && c.IsSupplier);
                if (nameExists != null && nameExists.Count > 0)
                    throw new SupplierNameInUseBadRequestException(_localizer);
            }

            var updated = new Contact
            {
                Id = old.Id,
                OrganizationId = old.OrganizationId,
                CreatedAt = old.CreatedAt,
                ModifiedAt = DateTime.UtcNow,
                Name = request.Name ?? old.Name,
                Company = old.Company,
                ContactPerson = request.ContactPerson ?? old.ContactPerson,
                Phone = request.Phone ?? old.Phone,
                Email = request.Email ?? old.Email,
                Address = request.Address ?? old.Address,
                Notes = request.Notes ?? old.Notes,
                Origin = old.Origin,
                IsActive = request.IsActive ?? old.IsActive,
                AssignedUserId = old.AssignedUserId,
                IsCustomer = old.IsCustomer,
                IsSupplier = true,
                LeadStatus = old.LeadStatus,
                LeadConvertedAt = old.LeadConvertedAt,
            };

            await _uow.ContactRepository.UpdateAsync(updated, old.Id);
            await _uow.CommitAsync();
        }
    }
}
