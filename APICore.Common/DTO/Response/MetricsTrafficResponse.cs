using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class MetricsTrafficResponse
    {
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public string Period { get; set; } = string.Empty;
        public int TotalVisits { get; set; }
        public int UniqueVisitors { get; set; }
        public double BounceRatePercent { get; set; }
        public double? AvgTimeOnCatalogSeconds { get; set; }
        public IList<MetricsTrafficSourceBreakdownResponse> TrafficSources { get; set; } = new List<MetricsTrafficSourceBreakdownResponse>();
        public IList<MetricsSearchTermResponse> TopSearchTerms { get; set; } = new List<MetricsSearchTermResponse>();
    }

    public class MetricsTrafficSourceBreakdownResponse
    {
        public string Source { get; set; } = string.Empty;
        public double Percent { get; set; }
    }

    public class MetricsSearchTermResponse
    {
        public string Term { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
