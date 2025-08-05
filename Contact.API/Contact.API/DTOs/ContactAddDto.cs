using System.Text.Json.Serialization;
using Contact.API.Models;

namespace Contact.API.DTOs
{
    public enum ContactType
    {
        PhoneNumber,
        EmailAddress,
        Location
    }

    public class ContactAddDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;

        public string Content { get; set; } = null!;
        public Guid PersonId { get; set; }
    }
}