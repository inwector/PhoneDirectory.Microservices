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

            var contactTypeList = _appDbContext.ContactInfos.Where(x=>x.Type == contactType).ToList();

            var persons = contactTypeList.Select(x=>x.PersonId).ToList();

            var phoneNumberCount = 0;

            foreach (var item in persons) {
                var count = _appDbContext.ContactInfos.Where(x => x.PersonId == item && x.Type.ToString() == "PhoneNumber").Count();
                phoneNumberCount += count;
            };

            var personCount = persons.Count();

            return Ok(new { personCount, phoneNumberCount }); // BUNU DÖNECEĞİZ REPORT.API'YE, REPORT.API DE REPORTS TABLOSUNU GÜNCELLEYECEK VE REPORTDETAILS TABLOSUNA KAYIT GİRECEK, EK OLARAK REPORT.API BUNU YOLLARKEN VERİ TABANINDA REPORTS TABLOSUNA KAYDI GİRMELİ Kİ KİŞİ BUNU ARADIĞINDA "VERİLER GELECEK" ŞEKLİNDE BİR MESAJ GÖREBİLSİN. REPORT.API UNIT TESTLERI VAR, O KADAR.
        }
    }
}
