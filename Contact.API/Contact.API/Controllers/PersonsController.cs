using Confluent.Kafka;
using Contact.API.Data;
using Contact.API.DTOs;
using Contact.API.Models;
using Contact.API.Services;
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
        private readonly IPersonService _personService;

        public PersonsController(AppDbContext context, IPersonService personService)
        {
            _context = context;
            _personService = personService;
        }

        // GET: api/persons
        [HttpGet]
        public async Task<IActionResult> GetAllPersons()
        {
            var persons = await _personService.GetAllAsync();
            return Ok(persons);
        }

        // GET: api/persons/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPerson(Guid id)
        {
            var person = await _personService.GetByIdAsync(id);
            if (person == null)
                return NotFound();
            return Ok(person);
        }

        // POST: api/persons
        [HttpPost]
        public async Task<IActionResult> CreatePerson([FromBody] PersonCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var person = new Person
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Company = dto.Company,
                ContactInfos = dto.ContactInfos.Select(ci => new ContactInfo
                {
                    Id = Guid.NewGuid(),
                    Content = ci.Content,
                    Type = (Models.ContactType)ci.Type
                }).ToList()
            };

            await _context.Persons.AddAsync(person);
            await _context.SaveChangesAsync();

            return Ok(new {personid = person.Id});
        }

        // DELETE: api/persons/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePerson(Guid id)
        {
            var deleted = await _personService.DeleteAsync(id);
            if (!deleted)
                return NotFound();
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
                .FirstOrDefault(ci => ci.Type == Models.ContactType.Location)?.Content;

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