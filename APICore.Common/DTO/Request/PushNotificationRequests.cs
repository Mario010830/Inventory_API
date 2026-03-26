using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class PushSubscriptionKeysRequest
    {
        [Required]
        public string P256dh { get; set; } = null!;

        [Required]
        public string Auth { get; set; } = null!;
    }

    public class PushSubscribeRequest
    {
        [Required]
        public string Endpoint { get; set; } = null!;

        public long? ExpirationTime { get; set; }

        [Required]
        public PushSubscriptionKeysRequest Keys { get; set; } = null!;

        [Required]
        public int LocationId { get; set; }
    }

    public class PushSendRequest
    {
        [Required]
        public int LocationId { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Body { get; set; } = null!;

        public string? Url { get; set; }
        public string? Image { get; set; }
        public string? ImageUrl { get; set; }
        public string? Tag { get; set; }
        public string? Icon { get; set; }
        public string? Badge { get; set; }
    }
}
