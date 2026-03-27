namespace APICore.Common.DTO.Response
{
    
    public class SubscriptionAdminContactResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}
