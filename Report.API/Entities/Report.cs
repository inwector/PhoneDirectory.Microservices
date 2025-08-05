using System;

namespace Report.API.Entities
{
    public enum ReportStatus
    {
        Preparing = 0,
        Completed = 1
    }

    public class Report
    {
        public Guid Id { get; set; }
        public DateTime RequestDate { get; set; }
        public ReportStatus Status { get; set; }

        public string Location { get; set; } = null!;
        public ICollection<ReportDetail> Details { get; set; } = new List<ReportDetail>();
    }
}