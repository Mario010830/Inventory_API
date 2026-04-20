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
    public class ContactService : IContactService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<IContactService> _localizer;

        public ContactService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<IContactService> localizer)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
        }

        public async Task<Contact> CreateContact(CreateContactRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var isCustomer = request.IsCustomer ?? true;
            var isSupplier = request.IsSupplier ?? false;
            var leadStatus = request.LeadStatus;
            EnsureValidRoles(isCustomer, isSupplier, leadStatus);

            var newContact = new Contact
            {
                OrganizationId = orgId,
                Name = request.Name,
                Company = request.Company,
                ContactPerson = request.ContactPerson,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                Notes = request.Notes,
                Origin = request.Origin,
                IsActive = request.IsActive,
                AssignedUserId = request.AssignedUserId,
                IsCustomer = isCustomer,
                IsSupplier = isSupplier,
                LeadStatus = leadStatus,
                LeadConvertedAt = null,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            await _uow.ContactRepository.AddAsync(newContact);
            await _uow.CommitAsync();

            return newContact;
        }

        public async Task<Contact> CreateCounterparty(CreateCounterpartyRequest request)
        {
            if (request.Roles == null || request.Roles.Count == 0)
                throw new ContactCounterpartyRolesRequiredBadRequestException(_localizer);

            var roles = request.Roles.Select(r => (r ?? "").Trim().ToLowerInvariant()).Where(r => r.Length > 0).ToHashSet();
            var isCustomer = roles.Contains("customer") || roles.Contains("lead");
            var isSupplier = roles.Contains("supplier");
            var leadStatus = roles.Contains("lead") ? (request.LeadStatus ?? "Nuevo") : request.LeadStatus;
            if (roles.Contains("lead") && string.IsNullOrWhiteSpace(leadStatus))
                leadStatus = "Nuevo";

            EnsureValidRoles(isCustomer, isSupplier, leadStatus);

            var merged = new CreateContactRequest
            {
                Name = request.Name,
                Company = request.Company,
                ContactPerson = request.ContactPerson,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                Notes = request.Notes,
                Origin = request.Origin,
                IsActive = request.IsActive,
                AssignedUserId = request.AssignedUserId,
                IsCustomer = isCustomer,
                IsSupplier = isSupplier,
                LeadStatus = leadStatus,
            };
            return await CreateContact(merged);
        }

        private void EnsureValidRoles(bool isCustomer, bool isSupplier, string? leadStatus)
        {
            if (!isCustomer && !isSupplier && string.IsNullOrWhiteSpace(leadStatus))
                throw new ContactCounterpartyRolesRequiredBadRequestException(_localizer);
        }

        public async Task DeleteContact(int id)
        {
            var contact = await _uow.ContactRepository.FirstOrDefaultAsync(c => c.Id == id);
            if (contact == null)
                throw new ContactNotFoundException(_localizer);

            _uow.ContactRepository.Delete(contact);
            await _uow.CommitAsync();
        }

        public async Task<PaginatedList<Contact>> GetAllContacts(int? page, int? perPage, string sortOrder = null, string? role = null)
        {
            var contacts = _uow.ContactRepository.GetAll();
            if (!string.IsNullOrWhiteSpace(role))
            {
                var r = role.Trim().ToLowerInvariant();
                contacts = r switch
                {
                    "customer" => contacts.Where(c => c.IsCustomer),
                    "supplier" => contacts.Where(c => c.IsSupplier),
                    "lead" => contacts.Where(c => c.LeadStatus != null && c.LeadConvertedAt == null),
                    _ => contacts,
                };
            }

            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<Contact>.CreateAsync(contacts, pageIndex, perPageIndex);
        }

        public async Task<Contact> GetContact(int id)
        {
            var contact = await _uow.ContactRepository.FirstOrDefaultAsync(c => c.Id == id);
            if (contact == null)
                throw new ContactNotFoundException(_localizer);
            return contact;
        }

        public async Task UpdateContact(int id, UpdateContactRequest request)
        {
            var oldContact = await _uow.ContactRepository.FirstOrDefaultAsync(c => c.Id == id);
            if (oldContact == null)
                throw new ContactNotFoundException(_localizer);

            var isCustomer = request.IsCustomer ?? oldContact.IsCustomer;
            var isSupplier = request.IsSupplier ?? oldContact.IsSupplier;
            var leadStatus = request.LeadStatus ?? oldContact.LeadStatus;
            if (request.IsCustomer.HasValue || request.IsSupplier.HasValue || request.LeadStatus != null)
                EnsureValidRoles(isCustomer, isSupplier, leadStatus);

            var updatedContact = new Contact
            {
                Id = oldContact.Id,
                OrganizationId = oldContact.OrganizationId,
                CreatedAt = oldContact.CreatedAt,
                ModifiedAt = DateTime.UtcNow,
                Name = request.Name ?? oldContact.Name,
                Company = request.Company ?? oldContact.Company,
                ContactPerson = request.ContactPerson ?? oldContact.ContactPerson,
                Phone = request.Phone ?? oldContact.Phone,
                Email = request.Email ?? oldContact.Email,
                Address = request.Address ?? oldContact.Address,
                Notes = request.Notes ?? oldContact.Notes,
                Origin = request.Origin ?? oldContact.Origin,
                IsActive = request.IsActive ?? oldContact.IsActive,
                AssignedUserId = request.AssignedUserId ?? oldContact.AssignedUserId,
                IsCustomer = isCustomer,
                IsSupplier = isSupplier,
                LeadStatus = leadStatus,
                LeadConvertedAt = oldContact.LeadConvertedAt,
            };

            await _uow.ContactRepository.UpdateAsync(updatedContact, oldContact.Id);
            await _uow.CommitAsync();
        }
    }
}
