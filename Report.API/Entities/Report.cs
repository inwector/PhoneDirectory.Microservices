using System;

namespace Report.API.Entities
{
    public enum ReportStatus
    {
        Preparing,
        Completed
    }

    public class Report
    {
        public Guid Id { get; set; }
        public DateTime RequestDate { get; set; }
        public ReportStatus Status { get; set; }

        public ICollection<ReportDetail> Details { get; set; } = new List<ReportDetail>();
    }
}