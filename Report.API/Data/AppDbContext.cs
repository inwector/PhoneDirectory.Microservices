using Microsoft.EntityFrameworkCore;
using Report.API.Entities;

namespace Report.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Report.API.Entities.Report> Reports => Set<Report.API.Entities.Report>();
        public DbSet<ReportDetail> ReportDetails => Set<ReportDetail>();
    }
}