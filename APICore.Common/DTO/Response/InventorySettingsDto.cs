namespace APICore.Common.DTO.Response
{
    public class InventorySettingsDto
    {
        public int RoundingDecimals { get; set; }
        public int PriceRoundingDecimals { get; set; }
        public bool AllowNegativeStock { get; set; }
        public string DefaultUnitOfMeasure { get; set; } = "unit";
    }
}
