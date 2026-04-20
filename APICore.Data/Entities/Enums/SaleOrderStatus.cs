namespace APICore.Data.Entities.Enums
{
    public enum SaleOrderStatus
    {
        draft,
        confirmed,
        /// <summary>Venta anulada por devolución total del pedido (el stock ya fue repuesto vía devoluciones).</summary>
        returned,
        cancelled
    }
}
