using APICore.Data.Entities;
using APICore.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks;

namespace APICore.Data.UoW
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly CoreDbContext _context;
        private IDbContextTransaction? _transaction;

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
            SaleOrderRepository ??= new GenericRepository<SaleOrder>(_context);
            SaleOrderItemRepository ??= new GenericRepository<SaleOrderItem>(_context);
            SaleReturnRepository ??= new GenericRepository<SaleReturn>(_context);
            SaleReturnItemRepository ??= new GenericRepository<SaleReturnItem>(_context);
            TagRepository ??= new GenericRepository<Tag>(_context);
            PromotionRepository ??= new GenericRepository<Promotion>(_context);
            PlanRepository ??= new GenericRepository<Plan>(_context);
            SubscriptionRepository ??= new GenericRepository<Subscription>(_context);
            SubscriptionRequestRepository ??= new GenericRepository<SubscriptionRequest>(_context);
            CurrencyRepository ??= new GenericRepository<Currency>(_context);
            BusinessCategoryRepository ??= new GenericRepository<BusinessCategory>(_context);
            ProductImageRepository ??= new GenericRepository<ProductImage>(_context);
            ProductTagRepository ??= new GenericRepository<ProductTag>(_context);
            ProductLocationOfferRepository ??= new GenericRepository<ProductLocationOffer>(_context);
            WebPushSubscriptionRepository ??= new GenericRepository<WebPushSubscription>(_context);
            DailySummaryRepository ??= new GenericRepository<DailySummary>(_context);
            DailySummaryInventoryItemRepository ??= new GenericRepository<DailySummaryInventoryItem>(_context);
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
        public IGenericRepository<SaleOrder> SaleOrderRepository { get; set; }
        public IGenericRepository<SaleOrderItem> SaleOrderItemRepository { get; set; }
        public IGenericRepository<SaleReturn> SaleReturnRepository { get; set; }
        public IGenericRepository<SaleReturnItem> SaleReturnItemRepository { get; set; }
        public IGenericRepository<Tag> TagRepository { get; set; }
        public IGenericRepository<Promotion> PromotionRepository { get; set; }
        public IGenericRepository<Plan> PlanRepository { get; set; }
        public IGenericRepository<Subscription> SubscriptionRepository { get; set; }
        public IGenericRepository<SubscriptionRequest> SubscriptionRequestRepository { get; set; }
        public IGenericRepository<Currency> CurrencyRepository { get; set; }
        public IGenericRepository<BusinessCategory> BusinessCategoryRepository { get; set; }
        public IGenericRepository<ProductImage> ProductImageRepository { get; set; }
        public IGenericRepository<ProductTag> ProductTagRepository { get; set; }
        public IGenericRepository<ProductLocationOffer> ProductLocationOfferRepository { get; set; }
        public IGenericRepository<WebPushSubscription> WebPushSubscriptionRepository { get; set; }
        public IGenericRepository<DailySummary> DailySummaryRepository { get; set; }
        public IGenericRepository<DailySummaryInventoryItem> DailySummaryInventoryItemRepository { get; set; }

        public async Task<int> CommitAsync()
        {
            var result = await _context.SaveChangesAsync();
            
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            
            return result;
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await operation();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}