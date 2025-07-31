using Contact.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Contact.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Person> Persons { get; set; }
        public DbSet<ContactInfo> ContactInfos { get; set; }
        public DbSet<LocationReport> LocationReports { get; set; }
    }
}
