using APICore.Data.Entities;
using APICore.Data.Repository;
using System;
using System.Threading.Tasks;

namespace APICore.Data.UoW
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly CoreDbContext _context;

        public UnitOfWork(CoreDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            UserRepository ??= new GenericRepository<User>(_context);
            UserTokenRepository ??= new GenericRepository<UserToken>(_context);
            SettingRepository ??= new GenericRepository<Setting>(_context);
            LogRepository ??= new GenericRepository<Log>(_context);
            ProductRepository ??= new GenericRepository<Product>(_context);
            ProductCategoryRepository ??= new GenericRepository<ProductCategory>(_context);
            SupplierRepository ??= new GenericRepository<Supplier>(_context);
            InventoryMovementRepository ??= new GenericRepository<InventoryMovement>(_context);
            InventoryRepository ??= new GenericRepository<Inventory>(_context);
            OrganizationRepository ??= new GenericRepository<Organization>(_context);
            LocationRepository ??= new GenericRepository<Location>(_context);
            RoleRepository ??= new GenericRepository<Role>(_context);
            PermissionRepository ??= new GenericRepository<Permission>(_context);
            RolePermissionRepository ??= new GenericRepository<RolePermission>(_context);
            ContactRepository ??= new GenericRepository<Contact>(_context);
            LeadRepository ??= new GenericRepository<Lead>(_context);
        }

        public IGenericRepository<User> UserRepository { get; set; }
        public IGenericRepository<UserToken> UserTokenRepository { get; set; }
        public IGenericRepository<Setting> SettingRepository { get; set; }
        public IGenericRepository<Log> LogRepository { get; set; }
        public IGenericRepository<Product> ProductRepository { get; set; }
        public IGenericRepository<Supplier> SupplierRepository { get; set; }
        public IGenericRepository<Inventory> InventoryRepository { get; set; }
        public IGenericRepository<InventoryMovement> InventoryMovementRepository { get; set; }
        public IGenericRepository<ProductCategory> ProductCategoryRepository { get; set; }
        public IGenericRepository<Organization> OrganizationRepository { get; set; }
        public IGenericRepository<Location> LocationRepository { get; set; }
        public IGenericRepository<Role> RoleRepository { get; set; }
        public IGenericRepository<Permission> PermissionRepository { get; set; }
        public IGenericRepository<RolePermission> RolePermissionRepository { get; set; }
        public IGenericRepository<Contact> ContactRepository { get; set; }
        public IGenericRepository<Lead> LeadRepository { get; set; }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}