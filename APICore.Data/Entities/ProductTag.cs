namespace APICore.Data.Entities
{
    /// <summary>
    /// Relación many-to-many entre Product y Tag.
    /// </summary>
    public class ProductTag
    {
        public int ProductId { get; set; }
        public int TagId { get; set; }

        public virtual Product Product { get; set; } = null!;
        public virtual Tag Tag { get; set; } = null!;
    }
}
