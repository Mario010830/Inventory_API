namespace APICore.Common.DTO.Request
{
    public class UpdateCurrencyRequest
    {
        public string? Name { get; set; }
        public decimal? ExchangeRate { get; set; }
        public bool? IsActive { get; set; }
    }
}
