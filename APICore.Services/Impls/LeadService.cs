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
    public class LeadService : ILeadService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<ILeadService> _localizer;

        public LeadService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<ILeadService> localizer)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
        }

        public async Task<Contact> ConvertToContact(int leadId)
        {
            var lead = await _uow.ContactRepository.FirstOrDefaultAsync(c =>
                c.Id == leadId && c.LeadStatus != null && !c.LeadConvertedAt.HasValue);
            if (lead == null)
                throw new LeadNotFoundException(_localizer);

            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            lead.LeadConvertedAt = DateTime.UtcNow;
            lead.LeadStatus = null;
            lead.IsCustomer = true;
            lead.ModifiedAt = DateTime.UtcNow;
            await _uow.ContactRepository.UpdateAsync(lead, lead.Id);
            await _uow.CommitAsync();

            return lead;
        }

        public async Task<Contact> CreateLead(CreateLeadRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var contact = new Contact
            {
                OrganizationId = orgId,
                Name = request.Name,
                Company = request.Company,
                ContactPerson = request.ContactPerson,
                Phone = request.Phone,
                Email = request.Email,
                Origin = request.Origin,
                Notes = request.Notes,
                Address = null,
                IsActive = true,
                AssignedUserId = request.AssignedUserId,
                IsCustomer = true,
                IsSupplier = false,
                LeadStatus = request.Status ?? "Nuevo",
                LeadConvertedAt = null,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            await _uow.ContactRepository.AddAsync(contact);
            await _uow.CommitAsync();

            return contact;
        }

        public async Task DeleteLead(int id)
        {
            var lead = await _uow.ContactRepository.FirstOrDefaultAsync(c =>
                c.Id == id && c.LeadStatus != null && !c.LeadConvertedAt.HasValue);
            if (lead == null)
                throw new LeadNotFoundException(_localizer);

            _uow.ContactRepository.Delete(lead);
            await _uow.CommitAsync();
        }

        public async Task<PaginatedList<Contact>> GetAllLeads(int? page, int? perPage, string? status = null, string sortOrder = null)
        {
            var leads = _uow.ContactRepository.GetAll().Where(c => c.LeadStatus != null && !c.LeadConvertedAt.HasValue);
            if (!string.IsNullOrWhiteSpace(status))
                leads = leads.Where(c => c.LeadStatus == status);

            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<Contact>.CreateAsync(leads, pageIndex, perPageIndex);
        }

        public async Task<Contact> GetLead(int id)
        {
            var lead = await _uow.ContactRepository.FirstOrDefaultAsync(c =>
                c.Id == id && (c.LeadConvertedAt.HasValue || (c.LeadStatus != null && !c.LeadConvertedAt.HasValue)));
            if (lead == null)
                throw new LeadNotFoundException(_localizer);
            return lead;
        }

        public async Task UpdateLead(int id, UpdateLeadRequest request)
        {
            var oldLead = await _uow.ContactRepository.FirstOrDefaultAsync(c =>
                c.Id == id && (c.LeadConvertedAt.HasValue || (c.LeadStatus != null && !c.LeadConvertedAt.HasValue)));
            if (oldLead == null)
                throw new LeadNotFoundException(_localizer);

            if (oldLead.LeadConvertedAt.HasValue)
            {
                if (request.Status != null && request.Status != "Convertido")
                    throw new LeadAlreadyConvertedBadRequestException(_localizer);
            }

            var updatedLead = new Contact
            {
                Id = oldLead.Id,
                OrganizationId = oldLead.OrganizationId,
                CreatedAt = oldLead.CreatedAt,
                ModifiedAt = DateTime.UtcNow,
                Name = request.Name ?? oldLead.Name,
                Company = request.Company ?? oldLead.Company,
                ContactPerson = request.ContactPerson ?? oldLead.ContactPerson,
                Phone = request.Phone ?? oldLead.Phone,
                Email = request.Email ?? oldLead.Email,
                Origin = request.Origin ?? oldLead.Origin,
                Notes = request.Notes ?? oldLead.Notes,
                AssignedUserId = request.AssignedUserId ?? oldLead.AssignedUserId,
                IsActive = oldLead.IsActive,
                Address = oldLead.Address,
                IsCustomer = oldLead.IsCustomer,
                IsSupplier = oldLead.IsSupplier,
                LeadStatus = request.Status ?? oldLead.LeadStatus,
                LeadConvertedAt = oldLead.LeadConvertedAt,
            };

            await _uow.ContactRepository.UpdateAsync(updatedLead, oldLead.Id);
            await _uow.CommitAsync();
        }
    }
}
