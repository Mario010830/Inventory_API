using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ILeadService
    {
        Task<Contact> ConvertToContact(int leadId);
        Task<Contact> CreateLead(CreateLeadRequest request);
        Task DeleteLead(int id);
        Task UpdateLead(int id, UpdateLeadRequest request);
        Task<Contact> GetLead(int id);
        Task<PaginatedList<Contact>> GetAllLeads(int? page, int? perPage, string? status = null, string sortOrder = null);
    }
}
