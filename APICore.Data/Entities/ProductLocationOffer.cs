namespace APICore.Data.Entities
{
    /// <summary>
    /// Disponibilidad de un producto elaborado en una tienda concreta (sin inventario).
    /// </summary>
    public class ProductLocationOffer : BaseEntity
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public int OrganizationId { get; set; }

        public virtual Product Product { get; set; } = null!;
        public virtual Location Location { get; set; } = null!;
        public virtual Organization Organization { get; set; } = null!;
    }
}
