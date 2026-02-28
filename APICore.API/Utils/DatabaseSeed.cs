using APICore.Common.Constants;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace APICore.API.Utils
{
    public static class DatabaseSeed
    {
        public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
        {
            var uow = serviceProvider.GetRequiredService<IUnitOfWork>();

            await SeedLocationsRolesAndPermissionsAsync(uow);
            await CreateDefaultUserAsync(uow);
        }

        private static async Task SeedLocationsRolesAndPermissionsAsync(IUnitOfWork uow)
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

            var permissionCodes = new[] {
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
                PermissionCodes.LocationRead, PermissionCodes.LocationCreate, PermissionCodes.LocationUpdate, PermissionCodes.LocationDelete,
                PermissionCodes.ContactRead, PermissionCodes.ContactCreate, PermissionCodes.ContactUpdate, PermissionCodes.ContactDelete,
                PermissionCodes.LeadRead, PermissionCodes.LeadCreate, PermissionCodes.LeadUpdate, PermissionCodes.LeadDelete
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

            var allPerms = await uow.PermissionRepository.GetAll().ToListAsync();
            foreach (var role in new[] { superAdminRole, adminRole })
            {
                var existingRolePerms = await uow.RolePermissionRepository.FindBy(rp => rp.RoleId == role.Id).ToListAsync();
                foreach (var perm in allPerms)
                {
                    if (existingRolePerms.Any(rp => rp.PermissionId == perm.Id)) continue;
                    await uow.RolePermissionRepository.AddAsync(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
                }
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
    }
}