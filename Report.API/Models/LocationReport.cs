namespace Report.API.Models
{
    public enum ReportStatus
    {
        Preparing,
        Completed
    }

    public class LocationReport
    {
        public Guid Id { get; set; }
        public DateTime RequestedDate { get; set; }
        public ReportStatus Status { get; set; }
        public string Location { get; set; } = null!;
        public int PersonCount { get; set; }
        public int PhoneNumberCount { get; set; }
    }
}