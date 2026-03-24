using System;

namespace APICore.Common.DTO.Response
{
    public class CurrencyResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal ExchangeRate { get; set; }
        public bool IsActive { get; set; }
        public bool IsBase { get; set; }
        public bool IsDefaultDisplay { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
