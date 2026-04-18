using System;

namespace APICore.Common.DTO.Response
{
    public class CurrencyDenominationResponse
    {
        public int Id { get; set; }
        public int CurrencyId { get; set; }
        public decimal Value { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
