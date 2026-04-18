using APICore.Common.Enums;
using APICore.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APICore.Data
{
    public class CoreDbContext : DbContext
    {
        public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
        {
        }
        public bool IgnoreLocationFilter { get; set; }
        public int CurrentLocationId { get; set; } = -1;
        public int CurrentOrganizationId { get; set; } = -1;
        public DbSet<User> Users { get; set; }
        public DbSet<Setting> Setting { get; set; }
        public DbSet<Log> Log { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<InventoryMovement> InventoryMovements { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<SaleOrder> SaleOrders { get; set; }
        public DbSet<SaleOrderItem> SaleOrderItems { get; set; }
        public DbSet<SaleReturn> SaleReturns { get; set; }
        public DbSet<SaleReturnItem> SaleReturnItems { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ProductTag> ProductTags { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionRequest> SubscriptionRequests { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<BusinessCategory> BusinessCategories { get; set; }
        public DbSet<WebPushSubscription> WebPushSubscriptions { get; set; }
        public DbSet<ProductLocationOffer> ProductLocationOffers { get; set; }
        public DbSet<MetricsEvent> MetricsEvents { get; set; }
        public DbSet<DailySummary> DailySummaries { get; set; }
        public DbSet<DailySummaryInventoryItem> DailySummaryInventoryItems { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanPayment> LoanPayments { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<SaleOrderPayment> SaleOrderPayments { get; set; }
        public DbSet<SaleOrderPaymentDenomination> SaleOrderPaymentDenominations { get; set; }
        public DbSet<CurrencyDenomination> CurrencyDenominations { get; set; }
        public DbSet<CashOutflow> CashOutflows { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductId);
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Location)
                .WithMany(l => l.Inventories)
                .HasForeignKey(i => i.LocationId);

            // Organization -> Locations
            modelBuilder.Entity<Location>()
                .HasOne(l => l.Organization)
                .WithMany(o => o.Locations)
                .HasForeignKey(l => l.OrganizationId);

            modelBuilder.Entity<Location>()
                .HasOne(l => l.BusinessCategory)
                .WithMany(bc => bc.Locations)
                .HasForeignKey(l => l.BusinessCategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WebPushSubscription>()
                .HasOne(s => s.Location)
                .WithMany()
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WebPushSubscription>()
                .HasOne(s => s.Organization)
                .WithMany()
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WebPushSubscription>()
                .HasIndex(s => s.Endpoint)
                .IsUnique();

            modelBuilder.Entity<BusinessCategory>(e =>
            {
                e.HasIndex(b => b.Slug).IsUnique();
            });

            modelBuilder.Entity<InventoryMovement>()
                .Property(m => m.Type)
                .HasConversion<string>();

            modelBuilder.Entity<InventoryMovement>()
                .Property(m => m.Reason)
                .HasColumnName("Cause");

            modelBuilder.Entity<InventoryMovement>()
                .HasOne(m => m.Product)
                .WithMany(p => p.InventoryMovements)
                .HasForeignKey(m => m.ProductId);

            modelBuilder.Entity<InventoryMovement>()
                .HasOne(m => m.Supplier)
                .WithMany(s => s.InventoryMovements)
                .HasForeignKey(m => m.SupplierId);

            modelBuilder.Entity<InventoryMovement>()
                .HasOne(m => m.Location)
                .WithMany(l => l.InventoryMovements)
                .HasForeignKey(m => m.LocationId);

            modelBuilder.Entity<InventoryMovement>()
                .HasOne(m => m.SaleOrder)
                .WithMany(s => s.InventoryMovements)
                .HasForeignKey(m => m.SaleOrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // SaleOrder
            modelBuilder.Entity<SaleOrder>()
                .Property(s => s.Status)
                .HasConversion<string>();

            modelBuilder.Entity<SaleOrder>()
                .HasOne(s => s.Organization)
                .WithMany()
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleOrder>()
                .HasOne(s => s.Location)
                .WithMany()
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleOrder>()
                .HasOne(s => s.Contact)
                .WithMany()
                .HasForeignKey(s => s.ContactId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleOrder>()
                .HasMany(s => s.Items)
                .WithOne(i => i.SaleOrder)
                .HasForeignKey(i => i.SaleOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleOrder>()
                .HasMany(s => s.Returns)
                .WithOne(r => r.SaleOrder)
                .HasForeignKey(r => r.SaleOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleOrder>()
                .HasMany(s => s.Payments)
                .WithOne(p => p.SaleOrder)
                .HasForeignKey(p => p.SaleOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentMethod>()
                .HasOne(pm => pm.Organization)
                .WithMany(o => o.PaymentMethods)
                .HasForeignKey(pm => pm.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaymentMethod>()
                .Property(pm => pm.InstrumentReference)
                .HasMaxLength(120);

            modelBuilder.Entity<SaleOrderPayment>()
                .HasOne(p => p.PaymentMethod)
                .WithMany(pm => pm.SaleOrderPayments)
                .HasForeignKey(p => p.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleOrderPayment>()
                .HasOne(p => p.Currency)
                .WithMany()
                .HasForeignKey(p => p.CurrencyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleOrderPayment>()
                .HasMany(p => p.Denominations)
                .WithOne(d => d.SaleOrderPayment)
                .HasForeignKey(d => d.SaleOrderPaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleOrderPayment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleOrderPayment>()
                .Property(p => p.AmountForeign)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SaleOrderPayment>()
                .Property(p => p.ExchangeRateSnapshot)
                .HasPrecision(18, 8);

            modelBuilder.Entity<SaleOrderPayment>()
                .Property(p => p.Reference)
                .HasMaxLength(120);

            modelBuilder.Entity<SaleOrderPaymentDenomination>()
                .Property(d => d.Kind)
                .HasConversion<string>();

            modelBuilder.Entity<SaleOrderPaymentDenomination>()
                .Property(d => d.Value)
                .HasPrecision(18, 4);

            modelBuilder.Entity<CurrencyDenomination>()
                .HasOne(d => d.Currency)
                .WithMany(c => c.Denominations)
                .HasForeignKey(d => d.CurrencyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CurrencyDenomination>()
                .Property(d => d.Value)
                .HasPrecision(18, 4);

            modelBuilder.Entity<CurrencyDenomination>()
                .HasIndex(d => new { d.CurrencyId, d.Value })
                .IsUnique();

            // SaleOrderItem
            modelBuilder.Entity<SaleOrderItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleOrderItem>()
                .HasOne(i => i.Promotion)
                .WithMany()
                .HasForeignKey(i => i.PromotionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // SaleReturn
            modelBuilder.Entity<SaleReturn>()
                .Property(r => r.Status)
                .HasConversion<string>();

            modelBuilder.Entity<SaleReturn>()
                .HasOne(r => r.Organization)
                .WithMany()
                .HasForeignKey(r => r.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleReturn>()
                .HasOne(r => r.Location)
                .WithMany()
                .HasForeignKey(r => r.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleReturn>()
                .HasMany(r => r.Items)
                .WithOne(i => i.SaleReturn)
                .HasForeignKey(i => i.SaleReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            // SaleReturnItem
            modelBuilder.Entity<SaleReturnItem>()
                .HasOne(i => i.SaleOrderItem)
                .WithMany()
                .HasForeignKey(i => i.SaleOrderItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleReturnItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductCategory>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Tag (global, sin organización)
            modelBuilder.Entity<Tag>(e =>
            {
                e.HasIndex(t => t.Name).IsUnique();
                e.HasIndex(t => t.Slug).IsUnique();
            });

            // ProductTag (many-to-many)
            modelBuilder.Entity<ProductTag>()
                .HasKey(pt => new { pt.ProductId, pt.TagId });
            modelBuilder.Entity<ProductTag>()
                .HasOne(pt => pt.Product)
                .WithMany(p => p.ProductTags)
                .HasForeignKey(pt => pt.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.ProductTags)
                .HasForeignKey(pt => pt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Organization)
                .WithMany(o => o.Products)
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Promotion>()
                .Property(p => p.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Promotion>()
                .HasOne(p => p.Organization)
                .WithMany()
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Promotion>()
                .HasOne(p => p.Product)
                .WithMany(pr => pr.Promotions)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductLocationOffer>()
                .HasIndex(o => new { o.ProductId, o.LocationId })
                .IsUnique();
            modelBuilder.Entity<ProductLocationOffer>()
                .HasOne(o => o.Product)
                .WithMany(p => p.LocationOffers)
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductLocationOffer>()
                .HasOne(o => o.Location)
                .WithMany()
                .HasForeignKey(o => o.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductLocationOffer>()
                .HasOne(o => o.Organization)
                .WithMany()
                .HasForeignKey(o => o.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .Property(p => p.Tipo)
                .HasConversion<string>()
                .HasDefaultValue(APICore.Data.Entities.Enums.ProductType.inventariable);
            modelBuilder.Entity<ProductCategory>()
                .HasOne(c => c.Organization)
                .WithMany(o => o.ProductCategories)
                .HasForeignKey(c => c.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Supplier>()
                .HasOne(s => s.Organization)
                .WithMany(o => o.Suppliers)
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contact>()
                .HasOne(c => c.Organization)
                .WithMany()
                .HasForeignKey(c => c.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Contact>()
                .HasOne(c => c.AssignedUser)
                .WithMany()
                .HasForeignKey(c => c.AssignedUserId)
                .IsRequired(false);

            modelBuilder.Entity<Lead>()
                .HasOne(l => l.Organization)
                .WithMany()
                .HasForeignKey(l => l.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Lead>()
                .HasOne(l => l.AssignedUser)
                .WithMany()
                .HasForeignKey(l => l.AssignedUserId)
                .IsRequired(false);
            modelBuilder.Entity<Lead>()
                .HasOne(l => l.ConvertedToContact)
                .WithMany()
                .HasForeignKey(l => l.ConvertedToContactId)
                .IsRequired(false);

            modelBuilder.Entity<Role>()
                .HasOne(r => r.Organization)
                .WithMany(o => o.Roles)
                .HasForeignKey(r => r.OrganizationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Role-Permission (many-to-many)
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            // User -> Role, User -> Location
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .IsRequired(false);
            modelBuilder.Entity<User>()
                .HasOne(u => u.Location)
                .WithMany(l => l.Users)
                .HasForeignKey(u => u.LocationId)
                .IsRequired(false);
            modelBuilder.Entity<User>()
                .HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId)
                .IsRequired(false);

        
            // SuperAdmin: IgnoreLocationFilter = true → ve todo.
            // Admin org (sin location): CurrentLocationId <= 0 → filtro por organización.
            // Usuario con location: CurrentLocationId > 0 → filtro solo por esa location.
            modelBuilder.Entity<Location>().HasQueryFilter(l =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && l.Id == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && l.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<User>().HasQueryFilter(u =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && u.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && (u.OrganizationId == CurrentOrganizationId || (u.Location != null && u.Location.OrganizationId == CurrentOrganizationId))));
            modelBuilder.Entity<Inventory>().HasQueryFilter(i =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && i.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && i.Location != null && i.Location.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<InventoryMovement>().HasQueryFilter(m =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && m.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && m.Location != null && m.Location.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<Product>().HasQueryFilter(p =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && p.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<ProductLocationOffer>().HasQueryFilter(o =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && o.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<ProductImage>().HasQueryFilter(pi =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && pi.Product != null && pi.Product.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<ProductCategory>().HasQueryFilter(c =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && c.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<PaymentMethod>().HasQueryFilter(pm =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && pm.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<Promotion>().HasQueryFilter(p =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && p.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<Supplier>().HasQueryFilter(s =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && s.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<Contact>().HasQueryFilter(c =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && c.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<Lead>().HasQueryFilter(l =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && l.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<Role>().HasQueryFilter(r =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && (r.OrganizationId == null || r.OrganizationId == CurrentOrganizationId)));
            modelBuilder.Entity<Setting>().HasQueryFilter(s =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && (s.OrganizationId == null || s.OrganizationId == CurrentOrganizationId)));
            modelBuilder.Entity<WebPushSubscription>().HasQueryFilter(s =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && s.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && s.OrganizationId == CurrentOrganizationId));
            modelBuilder.Entity<Log>().HasQueryFilter(l =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && (l.OrganizationId == null || l.OrganizationId == CurrentOrganizationId)));

            modelBuilder.Entity<SaleOrder>().HasQueryFilter(s =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && s.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && s.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<SaleOrderItem>().HasQueryFilter(i =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && i.SaleOrder != null && i.SaleOrder.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && i.SaleOrder != null && i.SaleOrder.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<SaleOrderPayment>().HasQueryFilter(p =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && p.SaleOrder != null && p.SaleOrder.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && p.SaleOrder != null && p.SaleOrder.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<SaleOrderPaymentDenomination>().HasQueryFilter(d =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && d.SaleOrderPayment != null && d.SaleOrderPayment.SaleOrder != null && d.SaleOrderPayment.SaleOrder.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && d.SaleOrderPayment != null && d.SaleOrderPayment.SaleOrder != null && d.SaleOrderPayment.SaleOrder.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<CurrencyDenomination>().HasQueryFilter(d =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && d.Currency != null && d.Currency.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<SaleReturn>().HasQueryFilter(r =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && r.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && r.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<SaleReturnItem>().HasQueryFilter(i =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && i.SaleReturn != null && i.SaleReturn.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && i.SaleReturn != null && i.SaleReturn.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<Plan>(e =>
            {
                e.HasIndex(p => p.Name).IsUnique();
            });

            modelBuilder.Entity<Currency>()
                .HasOne(c => c.Organization)
                .WithMany(o => o.Currencies)
                .HasForeignKey(c => c.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Currency>(e =>
            {
                e.HasIndex(c => new { c.OrganizationId, c.Code }).IsUnique();
                e.Property(c => c.ExchangeRate).HasPrecision(18, 8);
            });
            modelBuilder.Entity<Currency>().HasQueryFilter(c =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && c.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Subscriptions)
                .WithOne(s => s.Organization)
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Organization>()
                .HasOne(o => o.Subscription)
                .WithOne()
                .HasForeignKey<Organization>(o => o.SubscriptionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubscriptionRequest>()
                .HasOne(r => r.Subscription)
                .WithMany(s => s.Requests)
                .HasForeignKey(r => r.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MetricsEvent>()
                .HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MetricsEvent>()
                .HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MetricsEvent>()
                .HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MetricsEvent>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MetricsEvent>()
                .HasOne(e => e.SaleOrder)
                .WithMany()
                .HasForeignKey(e => e.SaleOrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MetricsEvent>()
                .HasIndex(e => new { e.OrganizationId, e.EventType, e.OccurredAt });
            modelBuilder.Entity<MetricsEvent>()
                .HasIndex(e => new { e.OrganizationId, e.OccurredAt });
            modelBuilder.Entity<MetricsEvent>()
                .HasIndex(e => e.EventType);
            modelBuilder.Entity<MetricsEvent>().HasQueryFilter(e =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && e.OrganizationId == CurrentOrganizationId));

            // DailySummary
            modelBuilder.Entity<DailySummary>()
                .HasOne(d => d.Location)
                .WithMany()
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailySummary>()
                .HasOne(d => d.Organization)
                .WithMany()
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailySummary>()
                .HasMany(d => d.InventoryItems)
                .WithOne(i => i.DailySummary)
                .HasForeignKey(i => i.DailySummaryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DailySummary>()
                .HasIndex(d => new { d.OrganizationId, d.LocationId, d.Date })
                .IsUnique();

            // DailySummaryInventoryItem
            modelBuilder.Entity<DailySummaryInventoryItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailySummary>().HasQueryFilter(d =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && d.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && d.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<DailySummaryInventoryItem>().HasQueryFilter(i =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && i.DailySummary != null && i.DailySummary.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && i.DailySummary != null && i.DailySummary.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<CashOutflow>()
                .HasOne(c => c.Organization)
                .WithMany()
                .HasForeignKey(c => c.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashOutflow>()
                .HasOne(c => c.Location)
                .WithMany()
                .HasForeignKey(c => c.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashOutflow>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<CashOutflow>()
                .Property(c => c.Date)
                .HasColumnType("date");
            modelBuilder.Entity<CashOutflow>()
                .Property(c => c.Amount)
                .HasPrecision(18, 2);
            modelBuilder.Entity<CashOutflow>()
                .HasIndex(c => new { c.OrganizationId, c.LocationId, c.Date });

            modelBuilder.Entity<CashOutflow>().HasQueryFilter(c =>
                IgnoreLocationFilter
                || (CurrentLocationId > 0 && c.LocationId == CurrentLocationId)
                || (CurrentLocationId <= 0 && CurrentOrganizationId > 0 && c.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Organization)
                .WithMany()
                .HasForeignKey(l => l.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.PrincipalCurrency)
                .WithMany()
                .HasForeignKey(l => l.PrincipalCurrencyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .Property(l => l.InterestRatePeriod)
                .HasConversion<string>()
                .HasDefaultValue(LoanInterestRatePeriod.annual);

            modelBuilder.Entity<LoanPayment>()
                .HasOne(p => p.Loan)
                .WithMany(l => l.Payments)
                .HasForeignKey(p => p.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Loan>().HasQueryFilter(l =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && l.OrganizationId == CurrentOrganizationId));

            modelBuilder.Entity<LoanPayment>().HasQueryFilter(p =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && p.Loan != null && p.Loan.OrganizationId == CurrentOrganizationId));
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // PostgreSQL (Npgsql): timestamptz exige DateTime con Kind=Utc; DateTime.Now es Local y provoca DbUpdateException.
            var currentDate = DateTime.UtcNow;

            var currentChanges = ChangeTracker.Entries<BaseEntity>();
            var currentChangedList = currentChanges.ToList();

            foreach (var entry in currentChangedList)
            {
                var entity = entry.Entity;

                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = currentDate;
                        entry.Entity.ModifiedAt = currentDate;
                        break;

                    case EntityState.Modified:
                        entry.Entity.ModifiedAt = currentDate;
                        entry.Entity.CreatedAt = entry.OriginalValues.GetValue<DateTime>("CreatedAt");
                        break;

                    case EntityState.Detached:
                        break;

                    case EntityState.Deleted:
                        break;

                    case EntityState.Unchanged:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}