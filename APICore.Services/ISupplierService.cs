using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ISupplierService
    {
        Task<Contact> CreateSupplier(CreateSupplierRequest request);
        Task DeleteSupplier(int id);
        Task UpdateSupplier(int id, UpdateSupplierRequest request);
        Task<Contact> GetSupplier(int id);
        Task<PaginatedList<Contact>> GetAllSuppliers(int? page, int? perPage, string sortOrder = null);
    }
}
