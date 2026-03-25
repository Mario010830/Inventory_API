using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response.Reports
{
    public class LeadRowDto
    {
        public int LeadId { get; set; }
        public string? Name { get; set; }
        public string? Company { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int? ConvertedToContactId { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public string? ContactName { get; set; }
    }

    public class CrmReportResponse : ReportResponse
    {
        public long TotalLeads { get; set; }
        public long ConvertedLeads { get; set; }
        public decimal ConversionRate { get; set; }
        public List<LeadRowDto> Leads { get; set; } = new();
    }
}

