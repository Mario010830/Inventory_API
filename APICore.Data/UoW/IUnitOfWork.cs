using APICore.Data.Entities;
using APICore.Data.Repository;
using System.Threading.Tasks;

namespace APICore.Data.UoW
{
    public interface IUnitOfWork
    {
        IGenericRepository<User> UserRepository { get; set; }
        IGenericRepository<UserToken> UserTokenRepository { get; set; }
        IGenericRepository<Setting> SettingRepository { get; set; }
        IGenericRepository<Log> LogRepository { get; set; }
        IGenericRepository<Product> ProductRepository { get; set; }
        IGenericRepository<ProductCategory> ProductCategoryRepository { get; set; }
        IGenericRepository<Inventory> InventoryRepository { get; set; }
        IGenericRepository<InventoryMovement> InventoryMovementRepository { get; set; }
        IGenericRepository<Supplier> SupplierRepository { get; set; }
        IGenericRepository<Organization> OrganizationRepository { get; set; }
        IGenericRepository<Location> LocationRepository { get; set; }
        IGenericRepository<Role> RoleRepository { get; set; }
        IGenericRepository<Permission> PermissionRepository { get; set; }
        IGenericRepository<RolePermission> RolePermissionRepository { get; set; }
        IGenericRepository<Contact> ContactRepository { get; set; }
        IGenericRepository<Lead> LeadRepository { get; set; }
        Task<int> CommitAsync();
    }
}