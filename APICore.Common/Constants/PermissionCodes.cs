namespace APICore.Common.Constants
{
    /// <summary>
    /// Códigos de permisos usados para autorización por endpoint.
    /// </summary>
    public static class PermissionCodes
    {
        public const string Admin = "admin";

        public const string ProductRead = "product.read";
        public const string ProductCreate = "product.create";
        public const string ProductUpdate = "product.update";
        public const string ProductDelete = "product.delete";

        public const string UserRead = "user.read";
        public const string UserCreate = "user.create";
        public const string UserUpdate = "user.update";
        public const string UserDelete = "user.delete";

        public const string InventoryRead = "inventory.read";
        public const string InventoryManage = "inventory.manage";

        public const string InventoryMovementRead = "inventorymovement.read";
        public const string InventoryMovementCreate = "inventorymovement.create";

        public const string SupplierRead = "supplier.read";
        public const string SupplierCreate = "supplier.create";
        public const string SupplierUpdate = "supplier.update";
        public const string SupplierDelete = "supplier.delete";

        public const string ProductCategoryRead = "productcategory.read";
        public const string ProductCategoryCreate = "productcategory.create";
        public const string ProductCategoryUpdate = "productcategory.update";
        public const string ProductCategoryDelete = "productcategory.delete";

        public const string LogRead = "log.read";

        public const string SettingRead = "setting.read";
        public const string SettingManage = "setting.manage";

        public const string RoleRead = "role.read";
        public const string RoleCreate = "role.create";
        public const string RoleUpdate = "role.update";
        public const string RoleDelete = "role.delete";

        public const string OrganizationRead = "organization.read";
        public const string OrganizationCreate = "organization.create";
        public const string OrganizationUpdate = "organization.update";
        public const string OrganizationDelete = "organization.delete";

        public const string LocationRead = "location.read";
        public const string LocationCreate = "location.create";
        public const string LocationUpdate = "location.update";
        public const string LocationDelete = "location.delete";

        public const string ContactRead = "contact.read";
        public const string ContactCreate = "contact.create";
        public const string ContactUpdate = "contact.update";
        public const string ContactDelete = "contact.delete";

        public const string LeadRead = "lead.read";
        public const string LeadCreate = "lead.create";
        public const string LeadUpdate = "lead.update";
        public const string LeadDelete = "lead.delete";
    }
}
