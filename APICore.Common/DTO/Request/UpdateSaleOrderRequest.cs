using System.Collections.Generic;

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

        /// <summary>
        /// Si no es null, reemplaza las líneas de pago. Lista vacía elimina todos los pagos del borrador.
        /// </summary>
        public List<CreateSaleOrderPaymentRequest>? Payments { get; set; }
    }
}
