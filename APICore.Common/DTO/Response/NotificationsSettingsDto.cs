namespace APICore.Common.DTO.Response
{
    public class NotificationsSettingsDto
    {
        public bool AlertOnLowStock { get; set; }
        public string LowStockRecipients { get; set; } = "";
    }
}
