namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Cantidad total de un producto en una ubicación (suma de todos los registros de inventario de ese producto en esa ubicación).
    /// </summary>
    public class ProductStockByLocationResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public string UnitOfMeasure { get; set; } = "unit";
    }
}
