using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ILeadService
    {
        Task<Lead> CreateLead(CreateLeadRequest request);
        Task DeleteLead(int id);
        Task UpdateLead(int id, UpdateLeadRequest request);
        Task<Lead> GetLead(int id);
        Task<PaginatedList<Lead>> GetAllLeads(int? page, int? perPage, string? status = null, string sortOrder = null);
        Task<Contact> ConvertToContact(int leadId);
    }
}
