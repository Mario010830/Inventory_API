using APICore.Data.Entities.Enums;
using System.Collections.Generic;

namespace APICore.Data.Entities
{
    public class Product : BaseEntity
    {
        public int OrganizationId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? CategoryId { get; set; }
        public decimal Precio { get; set; } = 0;
        public decimal Costo { get; set; } = 0;
        public string ImagenUrl { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsForSale { get; set; }

        /// <summary>Borrado lógico: el registro permanece por historial de ventas (FK) y no se lista en catálogos administrativos.</summary>
        public bool IsDeleted { get; set; }

        public ProductType Tipo { get; set; } = ProductType.inventariable;

        /// <summary>Producto cuyo inventario se descuenta al vender este artículo (ej. saco de harina). Un solo nivel; el padre no debe tener otro padre.</summary>
        public int? StockParentProductId { get; set; }

        /// <summary>Unidades de stock del padre (misma UOM que el padre) consumidas por cada 1 unidad vendida de este producto (ej. 1/55 si el padre es saco de 55 lb y aquí se vende por lb).</summary>
        public decimal? StockUnitsConsumedPerSaleUnit { get; set; }

        public ProductCategory? Category { get; set; }
        public Organization? Organization { get; set; }

        public Product? StockParentProduct { get; set; }
        public ICollection<Product> ChildStockProducts { get; set; } = new List<Product>();

        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

        public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

        public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();

        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

        public ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

        public ICollection<ProductLocationOffer> LocationOffers { get; set; } = new List<ProductLocationOffer>();
    }
}

