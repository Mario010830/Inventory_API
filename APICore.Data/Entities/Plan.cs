using System.Collections.Generic;

namespace APICore.Data.Entities
{
    public class Plan : BaseEntity
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public int MaxProducts { get; set; }
        public int MaxUsers { get; set; }
        public int MaxLocations { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal AnnualPrice { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
