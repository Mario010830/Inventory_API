namespace APICore.Common.DTO.Request
{
    public class CreateCurrencyRequest
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal ExchangeRate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
