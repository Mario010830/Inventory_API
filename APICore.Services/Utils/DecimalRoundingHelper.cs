using System;

namespace APICore.Services.Utils
{
    /// <summary>
    /// Punto único de redondeo para cantidades y precios. Usar en todos los servicios que persisten decimales.
    /// </summary>
    public static class DecimalRoundingHelper
    {
        /// <summary>
        /// Redondea el valor con la cantidad de decimales indicada (MidpointRounding.AwayFromZero).
        /// </summary>
        public static decimal Round(decimal value, int decimals)
        {
            if (decimals < 0) decimals = 0;
            if (decimals > 28) decimals = 28;
            return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Redondeo para cantidades/stock (usa RoundingDecimals del provider).
        /// </summary>
        public static decimal RoundQuantity(decimal value, int roundingDecimals)
        {
            return Round(value, roundingDecimals);
        }

        /// <summary>
        /// Redondeo para precios/costos (usa PriceRoundingDecimals del provider).
        /// </summary>
        public static decimal RoundPrice(decimal value, int priceRoundingDecimals)
        {
            return Round(value, priceRoundingDecimals);
        }
    }
}
