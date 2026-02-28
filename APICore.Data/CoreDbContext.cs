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

            modelBuilder.Entity<InventoryMovement>()
                .Property(m => m.Type)
                .HasConversion<string>();

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

            modelBuilder.Entity<ProductCategory>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Organization)
                .WithMany(o => o.Products)
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
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
            modelBuilder.Entity<ProductCategory>().HasQueryFilter(c =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && c.OrganizationId == CurrentOrganizationId));
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
            modelBuilder.Entity<Log>().HasQueryFilter(l =>
                IgnoreLocationFilter
                || (CurrentOrganizationId > 0 && (l.OrganizationId == null || l.OrganizationId == CurrentOrganizationId)));
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var currentDate = DateTime.Now;

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