using System;

namespace APICore.Common.DTO.Response
{
    public class MetricsSalesResponse
    {
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public string Period { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AvgOrderValue { get; set; }
        public double CartAbandonmentRatePercent { get; set; }
        public MetricsConversionFunnelResponse ConversionFunnel { get; set; } = new MetricsConversionFunnelResponse();
    }

    public class MetricsConversionFunnelResponse
    {
        public long Visits { get; set; }
        public long ProductViews { get; set; }
        public long AddedToCart { get; set; }
        public long Completed { get; set; }
    }
}
