using APICore.Common.Constants;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APICore.API.Utils
{
    public static class DatabaseSeed
    {
        public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
        {
            // Ámbito propio: CoreDbContext es scoped; así coincide con el inyectado en UnitOfWork.
            using var scope = serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            var uow = sp.GetRequiredService<IUnitOfWork>();
            var currencyService = sp.GetRequiredService<ICurrencyService>();
            var paymentMethodService = sp.GetRequiredService<IPaymentMethodService>();
            var dbContext = sp.GetRequiredService<CoreDbContext>();

            // Sin usuario HTTP el contexto queda con CurrentOrganizationId = -1; los filtros de tenant
            // ocultan Location/Role/User y el seed creía que no existían → duplicados en cada ejecución.
            var previousIgnoreLocation = dbContext.IgnoreLocationFilter;
            dbContext.IgnoreLocationFilter = true;
            try
            {
                await SeedLocationsRolesAndPermissionsAsync(uow, currencyService, paymentMethodService);
                await CreateDefaultUserAsync(uow);
                await SeedDefaultTagsAsync(uow);
            }
            finally
            {
                dbContext.IgnoreLocationFilter = previousIgnoreLocation;
            }
        }

        private static async Task SeedLocationsRolesAndPermissionsAsync(
            IUnitOfWork uow,
            ICurrencyService currencyService,
            IPaymentMethodService paymentMethodService)
        {
            var now = DateTime.UtcNow;

            var defaultOrganization = await uow.OrganizationRepository.FindBy(o => o.Code == "DEFAULT").FirstOrDefaultAsync();
            if (defaultOrganization == null)
            {
                defaultOrganization = new Organization
                {
                    Name = "Organización Principal",
                    Code = "DEFAULT",
                    Description = "Organización por defecto",
                    IsActive = true,
                    CreatedAt = now,
                    ModifiedAt = now
                };
                await uow.OrganizationRepository.AddAsync(defaultOrganization);
                await uow.CommitAsync();
            }

            var defaultLocation = await uow.LocationRepository.FindBy(l => l.Code == "DEFAULT").FirstOrDefaultAsync();
            if (defaultLocation == null)
            {
                defaultLocation = new Location
                {
                    OrganizationId = defaultOrganization.Id,
                    Name = "Almacén Principal",
                    Code = "DEFAULT",
                    Description = "Localización por defecto",
                    CreatedAt = now,
                    ModifiedAt = now
                };
                await uow.LocationRepository.AddAsync(defaultLocation);
                await uow.CommitAsync();
            }

            var superAdminRole = await uow.RoleRepository.FindBy(r => r.Name == RoleNames.SuperAdmin).FirstOrDefaultAsync();
            if (superAdminRole == null)
            {
                superAdminRole = new Role
                {
                    Name = RoleNames.SuperAdmin,
                    Description = "Super administrador: ve todo el sistema (todas las organizaciones y localizaciones)",
                    IsSystem = true,
                    CreatedAt = now,
                    ModifiedAt = now
                };
                await uow.RoleRepository.AddAsync(superAdminRole);
                await uow.CommitAsync();
            }

            var adminRole = await uow.RoleRepository.FindBy(r => r.Name == RoleNames.Admin).FirstOrDefaultAsync();
            if (adminRole == null)
            {
                adminRole = new Role
                {
                    Name = RoleNames.Admin,
                    Description = "Administrador de organización: ve todo lo de las localizaciones de su organización",
                    IsSystem = true,
                    CreatedAt = now,
                    ModifiedAt = now
                };
                await uow.RoleRepository.AddAsync(adminRole);
                await uow.CommitAsync();
            }

            var permissionCodes = new[]
            {
                PermissionCodes.Admin,

                PermissionCodes.ProductRead, PermissionCodes.ProductCreate, PermissionCodes.ProductUpdate, PermissionCodes.ProductDelete,

                PermissionCodes.UserRead, PermissionCodes.UserCreate, PermissionCodes.UserUpdate, PermissionCodes.UserDelete,

                PermissionCodes.InventoryRead, PermissionCodes.InventoryManage,

                PermissionCodes.InventoryMovementRead, PermissionCodes.InventoryMovementCreate,

                PermissionCodes.SupplierRead, PermissionCodes.SupplierCreate, PermissionCodes.SupplierUpdate, PermissionCodes.SupplierDelete,

                PermissionCodes.ProductCategoryRead, PermissionCodes.ProductCategoryCreate, PermissionCodes.ProductCategoryUpdate, PermissionCodes.ProductCategoryDelete,

                PermissionCodes.LogRead,

                PermissionCodes.SettingRead, PermissionCodes.SettingManage,

                PermissionCodes.RoleRead, PermissionCodes.RoleCreate, PermissionCodes.RoleUpdate, PermissionCodes.RoleDelete,

                PermissionCodes.OrganizationRead, PermissionCodes.OrganizationCreate, PermissionCodes.OrganizationUpdate, PermissionCodes.OrganizationDelete,
                PermissionCodes.OrganizationVerify,

                PermissionCodes.LocationRead, PermissionCodes.LocationCreate, PermissionCodes.LocationUpdate, PermissionCodes.LocationDelete,

                PermissionCodes.ContactRead, PermissionCodes.ContactCreate, PermissionCodes.ContactUpdate, PermissionCodes.ContactDelete,

                PermissionCodes.LeadRead, PermissionCodes.LeadCreate, PermissionCodes.LeadUpdate, PermissionCodes.LeadDelete,

                PermissionCodes.LoanRead, PermissionCodes.LoanCreate, PermissionCodes.LoanUpdate, PermissionCodes.LoanDelete,

                PermissionCodes.PaymentMethodRead, PermissionCodes.PaymentMethodCreate, PermissionCodes.PaymentMethodUpdate, PermissionCodes.PaymentMethodDelete,

                PermissionCodes.SaleRead, PermissionCodes.SaleCreate, PermissionCodes.SaleUpdate, PermissionCodes.SaleCancel,
                PermissionCodes.SaleReport, PermissionCodes.SaleReturnCreate,
                PermissionCodes.TagRead, PermissionCodes.TagCreate, PermissionCodes.TagUpdate, PermissionCodes.TagDelete,

                PermissionCodes.SubscriptionRead, PermissionCodes.SubscriptionManage,
                PermissionCodes.PlanRead, PermissionCodes.PlanManage,

                PermissionCodes.CurrencyRead, PermissionCodes.CurrencyCreate, PermissionCodes.CurrencyUpdate, PermissionCodes.CurrencyDelete,

                PermissionCodes.MetricsRead,

                PermissionCodes.ManualChatAsk,

                PermissionCodes.DailySummaryView, PermissionCodes.DailySummaryCreate, PermissionCodes.DailySummaryExport,
            };

            foreach (var code in permissionCodes)
            {
                var perm = await uow.PermissionRepository.FindBy(p => p.Code == code).FirstOrDefaultAsync();
                if (perm == null)
                {
                    perm = new Permission
                    {
                        Code = code,
                        Name = code,
                        Description = code,
                        CreatedAt = now,
                        ModifiedAt = now
                    };
                    await uow.PermissionRepository.AddAsync(perm);
                }
            }
            await uow.CommitAsync();

            await currencyService.EnsureBaseCurrencyForOrganizationAsync(defaultOrganization.Id);

            await SeedDefaultPaymentMethodsAsync(uow, paymentMethodService, defaultOrganization.Id, now);

            await SeedBusinessCategoriesAsync(uow, now);

            await SeedPlansAsync(uow, now);
            await EnsureDefaultOrganizationSubscriptionAsync(uow, defaultOrganization, now);

            var allPerms = await uow.PermissionRepository.GetAll().ToListAsync();
            foreach (var role in new[] { superAdminRole, adminRole })
            {
                var existingRolePerms = await uow.RolePermissionRepository.FindBy(rp => rp.RoleId == role.Id).ToListAsync();
                foreach (var perm in allPerms)
                {
                    if (role.Id == adminRole.Id && string.Equals(perm.Code, PermissionCodes.OrganizationVerify, StringComparison.Ordinal))
                        continue;
                    if (existingRolePerms.Any(rp => rp.PermissionId == perm.Id)) continue;
                    await uow.RolePermissionRepository.AddAsync(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
                }
            }
            await uow.CommitAsync();
        }

        private static async Task SeedPlansAsync(IUnitOfWork uow, DateTime now)
        {
            var seedPlans = new[]
            {
                new { Name = PlanNames.Free, DisplayName = "Free", Description = (string?)null, MaxProducts = 5, MaxUsers = 3, MaxLocations = 1, Monthly = 0m, Annual = 0m },
                new { Name = PlanNames.Pro, DisplayName = "Pro", Description = (string?)null, MaxProducts = 100, MaxUsers = 50, MaxLocations = 5, Monthly = 5000m, Annual = 60000m },
                new { Name = PlanNames.Enterprise, DisplayName = "Enterprise", Description = (string?)null, MaxProducts = -1, MaxUsers = -1, MaxLocations = -1, Monthly = 99.99m, Annual = 999.99m },
            };

            foreach (var p in seedPlans)
            {
                var existing = await uow.PlanRepository.FindBy(x => x.Name == p.Name).FirstOrDefaultAsync();
                if (existing != null)
                    continue;

                var plan = new Plan
                {
                    Name = p.Name,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    MaxProducts = p.MaxProducts,
                    MaxUsers = p.MaxUsers,
                    MaxLocations = p.MaxLocations,
                    MonthlyPrice = p.Monthly,
                    AnnualPrice = p.Annual,
                    IsActive = true,
                    CreatedAt = now,
                    ModifiedAt = now,
                };
                await uow.PlanRepository.AddAsync(plan);
            }

            await uow.CommitAsync();
        }

        private static async Task SeedBusinessCategoriesAsync(IUnitOfWork uow, DateTime now)
        {
            var seedRows = new (string Name, string Icon)[]
            {
                ("Perfumería", "sparkles"),
                ("Tienda general", "shopping-bag"),
                ("Ropa y accesorios", "shirt"),
                ("Restaurante", "utensils"),
                ("Heladería", "ice-cream"),
                ("Farmacia", "cross"),
                ("Electrónica", "cpu"),
                ("Hogar y decoración", "home"),
                ("Belleza y cuidado personal", "heart"),
                ("Panadería", "croissant"),
                ("Dulcería", "candy"),
                ("Cafetería", "coffee"),
                ("Bar", "wine"),
                ("Pizzería", "pizza"),
                ("Floristería", "flower"),
                ("Joyería y relojería", "gem"),
                ("Ferretería", "wrench"),
                ("Juguetería", "gamepad-2"),
                ("Tienda de mascotas", "paw-print"),
                ("Óptica", "glasses"),
                ("Artículos para bebés", "baby"),
                ("Gimnasio", "dumbbell"),
                ("Academia", "book-open"),
                ("Tienda de videojuegos", "joystick"),
                ("Servicio informático", "laptop"),
                ("Móviles y accesorios", "smartphone"),
                ("Marketing y publicidad", "megaphone"),
                ("Organización de eventos", "calendar"),
                ("Carnicería", "drumstick"),
                ("Pescadería", "fish"),
                ("Productos naturales", "leaf"),
                ("Otros", "more-horizontal"),
            };

            for (var i = 0; i < seedRows.Length; i++)
            {
                var (name, icon) = seedRows[i];
                var slug = GenerateBusinessCategorySeedSlug(name);
                var existing = await uow.BusinessCategoryRepository.FindBy(b => b.Slug == slug).FirstOrDefaultAsync();
                if (existing != null)
                    continue;

                await uow.BusinessCategoryRepository.AddAsync(new BusinessCategory
                {
                    Name = name,
                    Slug = slug,
                    Icon = icon,
                    IsActive = true,
                    SortOrder = i,
                    CreatedAt = now,
                    ModifiedAt = now
                });
            }

            await uow.CommitAsync();
        }

        private static string GenerateBusinessCategorySeedSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var normalized = name.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            var slug = sb.ToString().Normalize(NormalizationForm.FormC)
                .ToLowerInvariant()
                .Trim();
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
            slug = Regex.Replace(slug, @"-+", "-").Trim('-');
            return slug;
        }

        private static async Task EnsureDefaultOrganizationSubscriptionAsync(IUnitOfWork uow, Organization defaultOrganization, DateTime now)
        {
            if (defaultOrganization.SubscriptionId.HasValue)
                return;

            var existingForOrg = await uow.SubscriptionRepository
                .FindBy(s => s.OrganizationId == defaultOrganization.Id)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();
            if (existingForOrg != null)
            {
                defaultOrganization.SubscriptionId = existingForOrg.Id;
                defaultOrganization.IsActive = true;
                uow.OrganizationRepository.Update(defaultOrganization);
                await uow.CommitAsync();
                return;
            }

            var freePlan = await uow.PlanRepository.FindBy(p => p.Name.ToLower() == PlanNames.Free).FirstOrDefaultAsync();
            if (freePlan == null)
                return;

            var subscription = new Subscription
            {
                OrganizationId = defaultOrganization.Id,
                PlanId = freePlan.Id,
                BillingCycle = BillingCycle.Monthly,
                Status = SubscriptionStatus.Active,
                StartDate = now,
                EndDate = now.AddMonths(1),
                UpdatedAt = now,
                CreatedAt = now,
                ModifiedAt = now,
            };
            await uow.SubscriptionRepository.AddAsync(subscription);
            await uow.CommitAsync();

            defaultOrganization.SubscriptionId = subscription.Id;
            defaultOrganization.IsActive = true;
            uow.OrganizationRepository.Update(defaultOrganization);
            await uow.CommitAsync();
        }

        private static async Task SeedDefaultPaymentMethodsAsync(
            IUnitOfWork uow,
            IPaymentMethodService paymentMethodService,
            int organizationId,
            DateTime now)
        {
            await paymentMethodService.EnsureCashPaymentMethodExistsAsync(organizationId);

            var optionalDefaults = new[]
            {
                new { Name = "Transferencia", SortOrder = 1 },
                new { Name = "Zelle", SortOrder = 2 },
            };
            foreach (var d in optionalDefaults)
            {
                var exists = await uow.PaymentMethodRepository
                    .FindBy(pm => pm.OrganizationId == organizationId
                                  && pm.Name == d.Name
                                  && pm.InstrumentReference == null)
                    .AnyAsync();
                if (exists)
                    continue;

                await uow.PaymentMethodRepository.AddAsync(new PaymentMethod
                {
                    OrganizationId = organizationId,
                    Name = d.Name,
                    SortOrder = d.SortOrder,
                    IsActive = true,
                    CreatedAt = now,
                    ModifiedAt = now,
                });
            }

            await uow.CommitAsync();
        }

        private static async Task CreateDefaultUserAsync(IUnitOfWork uow)
        {
            var admin = await uow.UserRepository.FindBy(u => u.Email == "admin@email.com").FirstOrDefaultAsync();
            var superAdminRole = await uow.RoleRepository.FindBy(r => r.Name == RoleNames.SuperAdmin).FirstOrDefaultAsync();
            var defaultLocation = await uow.LocationRepository.FindBy(l => l.Code == "DEFAULT").FirstOrDefaultAsync();

            if (admin == null)
            {
                using (var hashAlgorithm = SHA256.Create())
                {
                    var byteValue = Encoding.UTF8.GetBytes("admin123");
                    var byteHash = hashAlgorithm.ComputeHash(byteValue);

                    admin = new User()
                    {
                        FullName = "SuperAdmin",
                        Email = "admin@email.com",
                        Password = Convert.ToBase64String(byteHash),
                        BirthDate = DateTime.UtcNow.AddYears(-30),
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow,
                        Status = StatusEnum.ACTIVE,
                        RoleId = superAdminRole?.Id,
                        LocationId = null,
                        OrganizationId = null
                    };

                    await uow.UserRepository.AddAsync(admin);
                    await uow.CommitAsync();
                }
            }
            else
            {
                // Migrar usuario existente a SuperAdmin si tiene el rol Admin antiguo (ahora queremos que el principal sea SuperAdmin)
                var adminRole = await uow.RoleRepository.FindBy(r => r.Name == RoleNames.Admin).FirstOrDefaultAsync();
                if (superAdminRole != null && admin.RoleId == adminRole?.Id)
                {
                    admin.RoleId = superAdminRole.Id;
                    admin.OrganizationId = null;
                    admin.FullName = admin.FullName == "Admin" ? "SuperAdmin" : admin.FullName;
                    uow.UserRepository.Update(admin);
                    await uow.CommitAsync();
                }
                else if (admin.RoleId == null && superAdminRole != null)
                {
                    admin.RoleId = superAdminRole.Id;
                    admin.OrganizationId = null;
                    uow.UserRepository.Update(admin);
                    await uow.CommitAsync();
                }
            }
        }

        private static async Task SeedDefaultTagsAsync(IUnitOfWork uow)
        {
            var now = DateTime.UtcNow;
            foreach (var (name, slug, color) in DefaultTagsSeedData.Tags)
            {
                var exists = await uow.TagRepository.FirstOrDefaultAsync(t => t.Slug == slug);
                if (exists != null)
                    continue;

                await uow.TagRepository.AddAsync(new Tag
                {
                    Name = name,
                    Slug = slug,
                    Color = color,
                    CreatedAt = now
                });
            }

            await uow.CommitAsync();
        }
    }
}