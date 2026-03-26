using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class WebPushSubscription : BaseEntity
    {
        [Required]
        public string Endpoint { get; set; } = null!;

        [Required]
        public string P256DH { get; set; } = null!;

        [Required]
        public string Auth { get; set; } = null!;

        public long? ExpirationTime { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        public bool IsActive { get; set; } = true;

        public Location? Location { get; set; }
        public Organization? Organization { get; set; }
    }
}
