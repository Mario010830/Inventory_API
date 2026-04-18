using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class SaleOrderPaymentDenominationLineRequest
    {
        [Range(0.0001, double.MaxValue)]
        public decimal Value { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
