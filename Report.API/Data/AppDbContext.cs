using Microsoft.EntityFrameworkCore;
using Report.API.Models;

namespace Report.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<LocationReport> LocationReports { get; set; } = null!;
    }
}