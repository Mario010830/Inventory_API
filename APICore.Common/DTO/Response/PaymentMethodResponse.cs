using System;

namespace APICore.Common.DTO.Response
{
    public class PaymentMethodResponse
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? InstrumentReference { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
