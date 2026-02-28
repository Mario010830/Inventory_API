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
            var lead = await _uow.LeadRepository.FirstOrDefaultAsync(l => l.Id == leadId);
            if (lead == null)
                throw new LeadNotFoundException(_localizer);

            if (lead.ConvertedToContactId.HasValue)
                throw new LeadAlreadyConvertedBadRequestException(_localizer);

            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var newContact = new Contact
            {
                OrganizationId = orgId,
                Name = lead.Name,
                Company = lead.Company,
                ContactPerson = lead.ContactPerson,
                Phone = lead.Phone,
                Email = lead.Email,
                Address = null,
                Notes = lead.Notes,
                Origin = lead.Origin,
                IsActive = true,
                AssignedUserId = lead.AssignedUserId,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            await _uow.ContactRepository.AddAsync(newContact);
            await _uow.CommitAsync();

            lead.ConvertedToContactId = newContact.Id;
            lead.ConvertedAt = DateTime.UtcNow;
            lead.Status = "Convertido";
            lead.ModifiedAt = DateTime.UtcNow;
            await _uow.LeadRepository.UpdateAsync(lead, lead.Id);
            await _uow.CommitAsync();

            return newContact;
        }

        public async Task<Lead> CreateLead(CreateLeadRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var newLead = new Lead
            {
                OrganizationId = orgId,
                Name = request.Name,
                Company = request.Company,
                ContactPerson = request.ContactPerson,
                Phone = request.Phone,
                Email = request.Email,
                Origin = request.Origin,
                Status = request.Status ?? "Nuevo",
                Notes = request.Notes,
                AssignedUserId = request.AssignedUserId,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            await _uow.LeadRepository.AddAsync(newLead);
            await _uow.CommitAsync();

            return newLead;
        }

        public async Task DeleteLead(int id)
        {
            var lead = await _uow.LeadRepository.FirstOrDefaultAsync(l => l.Id == id);
            if (lead == null)
                throw new LeadNotFoundException(_localizer);

            _uow.LeadRepository.Delete(lead);
            await _uow.CommitAsync();
        }

        public async Task<PaginatedList<Lead>> GetAllLeads(int? page, int? perPage, string? status = null, string sortOrder = null)
        {
            var leads = _uow.LeadRepository.GetAll();
            if (!string.IsNullOrWhiteSpace(status))
                leads = leads.Where(l => l.Status == status);

            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<Lead>.CreateAsync(leads, pageIndex, perPageIndex);
        }

        public async Task<Lead> GetLead(int id)
        {
            var lead = await _uow.LeadRepository.FirstOrDefaultAsync(l => l.Id == id);
            if (lead == null)
                throw new LeadNotFoundException(_localizer);
            return lead;
        }

        public async Task UpdateLead(int id, UpdateLeadRequest request)
        {
            var oldLead = await _uow.LeadRepository.FirstOrDefaultAsync(l => l.Id == id);
            if (oldLead == null)
                throw new LeadNotFoundException(_localizer);

            if (oldLead.ConvertedToContactId.HasValue)
            {
                if (request.Status != null && request.Status != "Convertido")
                    throw new LeadAlreadyConvertedBadRequestException(_localizer);
            }

            var updatedLead = new Lead
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
                Status = request.Status ?? oldLead.Status,
                Notes = request.Notes ?? oldLead.Notes,
                AssignedUserId = request.AssignedUserId ?? oldLead.AssignedUserId,
                ConvertedToContactId = oldLead.ConvertedToContactId,
                ConvertedAt = oldLead.ConvertedAt,
            };

            await _uow.LeadRepository.UpdateAsync(updatedLead, oldLead.Id);
            await _uow.CommitAsync();
        }
    }
}
