namespace APICore.Common.Constants
{
    /// <summary>
    /// Claves y valores por defecto para la tabla Setting.
    /// Usado por InventorySettingsProvider y por el endpoint de configuración agrupada.
    /// </summary>
    public static class SettingKeys
    {
        public const string InventoryPrefix = "Inventory.";

        public const string RoundingDecimals = "Inventory.RoundingDecimals";
        public const int RoundingDecimalsDefault = 2;

        public const string PriceRoundingDecimals = "Inventory.PriceRoundingDecimals";
        public const int PriceRoundingDecimalsDefault = 2;

        public const string AllowNegativeStock = "Inventory.AllowNegativeStock";
        public const bool AllowNegativeStockDefault = false;

        public const string DefaultUnitOfMeasure = "Inventory.DefaultUnitOfMeasure";
        public const string DefaultUnitOfMeasureDefault = "unit";

        public const string CompanyName = "Company.Name";
        public const string CompanyNameDefault = "";

        public const string CompanyTaxId = "Company.TaxId";
        public const string CompanyTaxIdDefault = "";

        public const string NotificationsAlertOnLowStock = "Notifications.AlertOnLowStock";
        public const bool NotificationsAlertOnLowStockDefault = true;

        public const string NotificationsLowStockRecipients = "Notifications.LowStockRecipients";
        public const string NotificationsLowStockRecipientsDefault = "";
    }
}
