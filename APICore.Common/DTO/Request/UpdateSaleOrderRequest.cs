namespace APICore.Common.DTO.Request
{
    /// <summary>
    /// Solo se puede actualizar una venta en estado draft.
    /// </summary>
    public class UpdateSaleOrderRequest
    {
        public int? ContactId { get; set; }
        public string? Notes { get; set; }
        public decimal? DiscountAmount { get; set; }
    }
}
