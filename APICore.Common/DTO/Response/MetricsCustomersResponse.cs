using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class MetricsCustomersResponse
    {
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public string Period { get; set; } = string.Empty;
        public int NewBuyers { get; set; }
        public int ReturningBuyers { get; set; }
        public double? RatingsAverage { get; set; }
        public IList<MetricsRatingBucketResponse> RatingsDistribution { get; set; } = new List<MetricsRatingBucketResponse>();
        public IList<MetricsReviewResponse> Reviews { get; set; } = new List<MetricsReviewResponse>();
    }

    public class MetricsRatingBucketResponse
    {
        public int Stars { get; set; }
        public int Count { get; set; }
    }

    public class MetricsReviewResponse
    {
        public int Id { get; set; }
        public string? Text { get; set; }
        public int? Rating { get; set; }
        public DateTime? AtUtc { get; set; }
    }
}
