using System;

namespace Contact.API.Models
{
    public enum ContactType
    {
        PhoneNumber,
        EmailAddress,
        Location
    }

    public class ContactInfo
    {
        public Guid Id { get; set; }
        public ContactType Type { get; set; }
        public string Content { get; set; } = null!;

        public Guid PersonId { get; set; }
        public Person Person { get; set; } = null!;

    }
}