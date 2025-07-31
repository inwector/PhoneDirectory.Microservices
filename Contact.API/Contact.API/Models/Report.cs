using System.ComponentModel.DataAnnotations;

namespace Contact.API.Models
{
    public class LocationReport
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime RequestedDate { get; set; }

        public ReportStatus Status { get; set; }

        public string Location { get; set; } = null!;

        public int PersonCount { get; set; }

        public int PhoneNumberCount { get; set; }
    }

    public enum ReportStatus
    {
        Preparing = 0,
        Completed = 1
    }
}