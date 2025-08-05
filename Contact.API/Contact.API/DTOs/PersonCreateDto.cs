using Contact.API.Models;
using System.ComponentModel.DataAnnotations;

namespace Contact.API.DTOs
{
    public class PersonCreateDto
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Company { get; set; } = string.Empty;

        public List<ContactInfoCreateDto> ContactInfos { get; set; } = new();
    }

    public class ContactInfoCreateDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public ContactType Type { get; set; }
    }
}