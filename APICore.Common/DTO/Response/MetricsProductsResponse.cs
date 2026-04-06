using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class MetricsProductsResponse
    {
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public string Period { get; set; } = string.Empty;
        public IList<MetricsProductViewsResponse> MostViewedProducts { get; set; } = new List<MetricsProductViewsResponse>();
        public IList<MetricsProductFavoritesResponse> TopFavorited { get; set; } = new List<MetricsProductFavoritesResponse>();
        public IList<MetricsProductNoSalesResponse> ProductsWithNoSales { get; set; } = new List<MetricsProductNoSalesResponse>();
        public IList<MetricsProductViewToCartResponse> ViewToCartRate { get; set; } = new List<MetricsProductViewToCartResponse>();
    }

    public class MetricsProductViewsResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ViewCount { get; set; }
    }

    public class MetricsProductFavoritesResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int FavoriteCount { get; set; }
    }

    public class MetricsProductNoSalesResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class MetricsProductViewToCartResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int AddToCartCount { get; set; }
        public double ViewToCartRatePercent { get; set; }
    }
}
