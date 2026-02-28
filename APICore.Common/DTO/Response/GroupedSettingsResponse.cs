namespace APICore.Common.DTO.Response
{
    public class GroupedSettingsResponse
    {
        public InventorySettingsDto Inventory { get; set; } = new();
        public CompanySettingsDto Company { get; set; } = new();
        public NotificationsSettingsDto Notifications { get; set; } = new();
    }
}
