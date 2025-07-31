using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Report.API.Data;
using Report.API.Entities;
using Confluent.Kafka;

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
        public async Task<IActionResult> RequestReport([FromBody] string location)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };

            using var producer = new ProducerBuilder<Null, string>(config).Build();

            try
            {
                var result = await producer.ProduceAsync("report-requests", new Message<Null, string> { Value = location });

                return Ok(new { Message = $"Report request for '{location}' sent to Kafka." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending report request to Kafka: {ex.Message}");
            }
        }
    }
}