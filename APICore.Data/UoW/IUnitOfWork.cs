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
        IGenericRepository<SaleOrder> SaleOrderRepository { get; set; }
        IGenericRepository<SaleOrderItem> SaleOrderItemRepository { get; set; }
        IGenericRepository<SaleReturn> SaleReturnRepository { get; set; }
        IGenericRepository<SaleReturnItem> SaleReturnItemRepository { get; set; }
        IGenericRepository<Tag> TagRepository { get; set; }
        IGenericRepository<Promotion> PromotionRepository { get; set; }
        IGenericRepository<Plan> PlanRepository { get; set; }
        IGenericRepository<Subscription> SubscriptionRepository { get; set; }
        IGenericRepository<SubscriptionRequest> SubscriptionRequestRepository { get; set; }
        IGenericRepository<Currency> CurrencyRepository { get; set; }
        IGenericRepository<BusinessCategory> BusinessCategoryRepository { get; set; }
        IGenericRepository<ProductImage> ProductImageRepository { get; set; }
        IGenericRepository<ProductTag> ProductTagRepository { get; set; }
        IGenericRepository<ProductLocationOffer> ProductLocationOfferRepository { get; set; }
        IGenericRepository<WebPushSubscription> WebPushSubscriptionRepository { get; set; }
        Task<int> CommitAsync();
        Task BeginTransactionAsync();
        Task RollbackAsync();
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }
}