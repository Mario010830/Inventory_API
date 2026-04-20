namespace APICore.Data.Entities
{
    public class LoyaltySettings : BaseEntity
    {
        public int OrganizationId { get; set; }
        public int PointsPerOrder { get; set; } = 1;
        public int NotifyEveryNOrders { get; set; } = 5;

        public virtual Organization Organization { get; set; } = null!;
    }
}
