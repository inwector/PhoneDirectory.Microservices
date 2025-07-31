using System;
using System.Collections.Generic;

namespace Contact.API.Models
{
    public class Person
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Company { get; set; }

        public ICollection<ContactInfo> ContactInfos { get; set; } = new List<ContactInfo>();
    }
}