using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CreateCurrencyDenominationRequest
    {
        [Range(0.0001, double.MaxValue)]
        public decimal Value { get; set; }

        public int SortOrder { get; set; }
    }
}
