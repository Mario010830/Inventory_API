namespace APICore.Data.Entities
{
    /// <summary>
    /// Inventario de un producto en un almacén. El mismo producto puede tener varios inventarios (uno por Location).
    /// </summary>
    public class Inventory : BaseEntity
    {
        public int ProductId { get; set; }
        /// <summary>
        /// Almacén/localización donde está este inventario. Usado para data scoping.
        /// </summary>
        public int LocationId { get; set; }
        public decimal CurrentStock { get; set; } = 0;
        public decimal MinimumStock { get; set; } = 0;
        public string UnitOfMeasure { get; set; } = "unit";

        public Product Product { get; set; } = null!;
        public Location Location { get; set; } = null!;
    }
}