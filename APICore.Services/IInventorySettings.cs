namespace APICore.Services
{
    /// <summary>
    /// Settings de inventario (redondeo, stock negativo, unidad por defecto).
    /// Implementación cacheada; los servicios usan esta interfaz en lugar de leer Setting por clave.
    /// </summary>
    public interface IInventorySettings
    {
        int RoundingDecimals { get; }
        int PriceRoundingDecimals { get; }
        bool AllowNegativeStock { get; }
        string DefaultUnitOfMeasure { get; }

        /// <summary>
        /// Invalida la caché para que la próxima lectura recargue desde BD (llamar tras actualizar settings de inventario).
        /// </summary>
        void InvalidateCache();
    }
}
