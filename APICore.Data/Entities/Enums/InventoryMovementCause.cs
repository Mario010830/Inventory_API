namespace APICore.Data.Entities.Enums
{
    /// <summary>
    /// Causa específica del movimiento de inventario.
    /// Complementa al Type (entry/exit/adjustment) para saber el motivo real.
    /// Causas de entrada: purchase, customer_return, transfer_in, initial_stock
    /// Causas de salida: sale, damage, internal_use, supplier_return, transfer_out, expiration
    /// Causas de ajuste: inventory_count, correction
    /// </summary>
    public enum InventoryMovementCause
    {
        // Entry causes
        purchase,
        customer_return,
        transfer_in,
        initial_stock,

        // Exit causes
        sale,
        damage,
        internal_use,
        supplier_return,
        transfer_out,
        expiration,

        // Adjustment causes
        inventory_count,
        correction
    }
}
