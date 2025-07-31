using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Report.API.Data;
using Report.API.Entities;

namespace Report.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/reports
        [HttpGet]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _context.Reports
                .Include(r => r.Details) // Detayları da dahil et
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return Ok(reports);
        }

        // GET: api/reports/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetReport(Guid id)
        {
            var report = await _context.Reports
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return NotFound();

            return Ok(report);
        }

        // POST: api/reports
        [HttpPost]
        public async Task<IActionResult> RequestReport()
        {
            var report = new Report.API.Entities.Report
            {
                Id = Guid.NewGuid(),
                RequestDate = DateTime.UtcNow,
                Status = ReportStatus.Preparing,
                Details = new List<ReportDetail>() // Başlangıçta boş liste
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
        }
    }
}