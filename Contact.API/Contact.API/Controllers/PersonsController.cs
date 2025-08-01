using Confluent.Kafka;
using Contact.API.Data;
using Contact.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PersonsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/persons
        [HttpGet]
        public async Task<IActionResult> GetAllPersons()
        {
            var persons = await _context.Persons
                .Include(p => p.ContactInfos)
                .ToListAsync();

            return Ok(persons);
        }

        // GET: api/persons/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPerson(Guid id)
        {
            var person = await _context.Persons
                .Include(p => p.ContactInfos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (person == null)
                return NotFound();

            return Ok(person);
        }

        // POST: api/persons
        [HttpPost]
        public async Task<IActionResult> CreatePerson([FromBody] Person person)
        {
            person.Id = Guid.NewGuid();
            _context.Persons.Add(person);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPerson), new { id = person.Id }, person);
        }

        // DELETE: api/persons/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePerson(Guid id)
        {
            var person = await _context.Persons
                .Include(p => p.ContactInfos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (person == null)
                return NotFound();

            _context.ContactInfos.RemoveRange(person.ContactInfos);
            _context.Persons.Remove(person);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/persons/{personId}/request-report
        [HttpPost("{personId}/request-report")]
        public async Task<IActionResult> RequestReport(Guid personId, [FromServices] IProducer<Null, string> kafkaProducer)
        {
            var person = await _context.Persons
                .Include(p => p.ContactInfos)
                .FirstOrDefaultAsync(p => p.Id == personId);

            if (person == null)
                return NotFound($"Person with id {personId} not found.");

            var locationInfo = person.ContactInfos
                .FirstOrDefault(ci => ci.Type == ContactType.Location)?.Content;

            if (string.IsNullOrEmpty(locationInfo))
                return BadRequest("No location info found for this person.");

            var reportRequest = new
            {
                Location = locationInfo
            };

            var jsonMessage = JsonSerializer.Serialize(reportRequest);

            await kafkaProducer.ProduceAsync("report-requests", new Message<Null, string>
            {
                Value = jsonMessage
            });

            return Ok(new { message = $"Report request for '{locationInfo}' sent to Kafka." });
        }

    }
}