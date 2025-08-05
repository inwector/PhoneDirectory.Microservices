using Contact.API.Data;
using Contact.API.DTOs;
using Contact.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/location")]
    public class LocationController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        
        public LocationController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        // POST: api/location/{locationName}
        [HttpPost]
        [Route("{location}")]
        public async Task<ActionResult> Stats(string location, CancellationToken cancellationToken)
        {

            var contactType = Enum.TryParse<Models.ContactType>(location, ignoreCase: true, out var parsedEnum)
            ? parsedEnum
            : Models.ContactType.Location;

            var contactTypeList = _appDbContext.ContactInfos.Where(x=>x.Type == contactType && x.Content == location).ToList();

            var persons = contactTypeList.Select(x=>x.PersonId).Distinct().ToList();

            var phoneNumberCount = 0;

            foreach (var item in persons) {
                var count = _appDbContext.ContactInfos.Where(x => x.PersonId == item && x.Type == Models.ContactType.PhoneNumber).Count();
                phoneNumberCount += count;
            };

            var personCount = persons.Count();

            return Ok(new { location, personCount, phoneNumberCount });
        }
    }
}
