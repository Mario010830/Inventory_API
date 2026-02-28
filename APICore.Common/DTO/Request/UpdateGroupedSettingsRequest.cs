namespace APICore.Common.DTO.Request
{
    /// <summary>
    /// Body para PUT de configuración agrupada. El front puede enviar solo las secciones que desea actualizar.
    /// </summary>
    public class UpdateGroupedSettingsRequest
    {
        public InventorySettingsUpdateDto? Inventory { get; set; }
        public CompanySettingsUpdateDto? Company { get; set; }
        public NotificationsSettingsUpdateDto? Notifications { get; set; }
    }

    public class InventorySettingsUpdateDto
    {
        public int? RoundingDecimals { get; set; }
        public int? PriceRoundingDecimals { get; set; }
        public bool? AllowNegativeStock { get; set; }
        public string? DefaultUnitOfMeasure { get; set; }
    }

    public class CompanySettingsUpdateDto
    {
        public string? Name { get; set; }
        public string? TaxId { get; set; }
    }

    public class NotificationsSettingsUpdateDto
    {
        public bool? AlertOnLowStock { get; set; }
        public string? LowStockRecipients { get; set; }
    }
}
