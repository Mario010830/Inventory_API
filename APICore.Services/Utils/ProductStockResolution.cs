using APICore.Data.Entities;

namespace APICore.Services.Utils
{
    /// <summary>
    /// Venta por unidad distinta al stock físico (ej. libra vs saco): convierte cantidad vendida → unidades de inventario del padre.
    /// </summary>
    public static class ProductStockResolution
    {
        public static bool UsesParentStock(Product product)
        {
            if (product == null)
                return false;
            return product.StockParentProductId is > 0
                && product.StockUnitsConsumedPerSaleUnit is decimal f
                && f > 0;
        }

        public static (int StockProductId, decimal StockQuantity) GetDeductionUnits(
            Product product,
            decimal saleQuantity,
            int roundingDecimals)
        {
            if (UsesParentStock(product))
            {
                var qty = DecimalRoundingHelper.RoundQuantity(
                    saleQuantity * product.StockUnitsConsumedPerSaleUnit!.Value,
                    roundingDecimals);
                return (product.StockParentProductId!.Value, qty);
            }

            return (product.Id, DecimalRoundingHelper.RoundQuantity(saleQuantity, roundingDecimals));
        }

        /// <summary>Stock disponible expresado en unidades de venta del hijo, a partir del stock del padre.</summary>
        public static decimal GetSaleUnitsFromParentStock(
            decimal parentStock,
            decimal stockUnitsConsumedPerSaleUnit,
            int roundingDecimals)
        {
            if (stockUnitsConsumedPerSaleUnit <= 0)
                return 0;
            return DecimalRoundingHelper.RoundQuantity(
                parentStock / stockUnitsConsumedPerSaleUnit,
                roundingDecimals);
        }
    }
}
