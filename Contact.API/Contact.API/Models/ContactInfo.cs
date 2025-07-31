using System;

namespace Contact.API.Models
{
    public enum ContactType
    {
        PhoneNumber,   // Telefon Numarası
        EmailAddress,  // E-mail Adresi
        Location       // Konum
    }

    public class ContactInfo
    {
        public Guid Id { get; set; }            // UUID
        public ContactType Type { get; set; }  // Bilgi Tipi
        public string Content { get; set; } = null!;  // Bilgi İçeriği (Numara, e-posta ya da konum)

        public Guid PersonId { get; set; }     // Yabancı anahtar - hangi kişiye ait
        public Person Person { get; set; } = null!;
    }
}