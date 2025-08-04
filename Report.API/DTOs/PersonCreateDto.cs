namespace Report.API.DTOs
{
    public class PersonCreateDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public List<ContactInfoCreateDto>? ContactInfos { get; set; }
    }
}
