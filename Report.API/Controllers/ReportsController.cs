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
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(AppDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/reports
        [HttpGet]
        public async Task<IActionResult> GetReports()
        {
            try
            {
                var reports = await _context.Reports
                    .Include(r => r.Details)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports");
                return StatusCode(500, "Error retrieving reports");
            }
        }

        // GET: api/reports/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetReport(Guid id)
        {
            try
            {
                var report = await _context.Reports
                    .Include(r => r.Details)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (report == null)
                    return NotFound($"Report with ID {id} not found");

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report with ID: {ReportId}", id);
                return StatusCode(500, "Error retrieving report");
            }
        }

        // POST: api/reports
        [HttpPost]
        public async Task<IActionResult> RequestReport([FromBody] CreateReportRequest request)
        {
            // Input validation
            if (request == null || string.IsNullOrWhiteSpace(request.Location))
            {
                return BadRequest("Location is required");
            }

            _logger.LogInformation("Received report request for location: {Location}", request.Location);

            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };

            using var producer = new ProducerBuilder<Null, string>(config).Build();

            try
            {
                var result = await producer.ProduceAsync("report-requests",
                    new Message<Null, string> { Value = request.Location });

                _logger.LogInformation("Report request sent to Kafka for location: {Location}", request.Location);

                return Ok(new
                {
                    Message = $"Report request for '{request.Location}' sent successfully.",
                    Location = request.Location,
                    RequestId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending report request for location: {Location}", request.Location);

                if (ex.Message.Contains("Broker") || ex.Message.Contains("Connection"))
                {
                    return StatusCode(500, "Kafka connection error. Please try again later.");
                }

                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }

    public class CreateReportRequest
    {
        public string Location { get; set; } = string.Empty;
    }
}