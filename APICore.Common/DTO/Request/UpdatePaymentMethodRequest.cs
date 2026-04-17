using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class UpdatePaymentMethodRequest
    {
        [MaxLength(120)]
        public string? Name { get; set; }

        public int? SortOrder { get; set; }

        public bool? IsActive { get; set; }

        /// <summary>
        /// Si se envía (incluido string vacío), actualiza o borra la referencia; si se omite en JSON, no cambia.
        /// </summary>
        [MaxLength(120)]
        public string? InstrumentReference { get; set; }
    }
}
