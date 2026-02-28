using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IContactService
    {
        Task<Contact> CreateContact(CreateContactRequest request);
        Task DeleteContact(int id);
        Task UpdateContact(int id, UpdateContactRequest request);
        Task<Contact> GetContact(int id);
        Task<PaginatedList<Contact>> GetAllContacts(int? page, int? perPage, string sortOrder = null);
    }
}
