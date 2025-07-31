using Contact.API.Data;
using Contact.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/persons/{personId}/[controller]")]
    public class ContactInfosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ContactInfosController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/persons/{personId}/contactinfos
        [HttpPost]
        public async Task<IActionResult> AddContactInfo(Guid personId, [FromBody] ContactInfo contactInfo)
        {
            var person = await _context.Persons.FindAsync(personId);
            if (person == null) return NotFound($"Person with Id {personId} not found.");

            contactInfo.Id = Guid.NewGuid();
            contactInfo.PersonId = personId;

            _context.ContactInfos.Add(contactInfo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetContactInfo), new { personId = personId, id = contactInfo.Id }, contactInfo);
        }

        // GET: api/persons/{personId}/contactinfos/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContactInfo(Guid personId, Guid id)
        {
            var contactInfo = await _context.ContactInfos
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.PersonId == personId);

            if (contactInfo == null) return NotFound();

            return Ok(contactInfo);
        }

        // DELETE: api/persons/{personId}/contactinfos/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContactInfo(Guid personId, Guid id)
        {
            var contactInfo = await _context.ContactInfos
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.PersonId == personId);

            if (contactInfo == null) return NotFound();

            _context.ContactInfos.Remove(contactInfo);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}