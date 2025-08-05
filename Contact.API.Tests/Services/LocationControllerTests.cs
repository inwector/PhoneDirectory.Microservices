using Contact.API.Controllers;
using Contact.API.Data;
using Contact.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Contact.API.Tests
{
    public class LocationControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Stats_ReturnsCorrectCounts_ForGivenLocation()
        {
            var context = GetDbContext();

            var person1 = new Person { Id = Guid.NewGuid(), FirstName = "Ali", LastName = "Veli" };
            var person2 = new Person { Id = Guid.NewGuid(), FirstName = "Ayşe", LastName = "Kara" };

            context.Persons.AddRange(person1, person2);

            context.ContactInfos.AddRange(
                new ContactInfo { Id = Guid.NewGuid(), PersonId = person1.Id, Type = ContactType.Location, Content = "Istanbul" },
                new ContactInfo { Id = Guid.NewGuid(), PersonId = person2.Id, Type = ContactType.Location, Content = "Istanbul" },
                new ContactInfo { Id = Guid.NewGuid(), PersonId = person1.Id, Type = ContactType.PhoneNumber, Content = "555-1111" },
                new ContactInfo { Id = Guid.NewGuid(), PersonId = person1.Id, Type = ContactType.PhoneNumber, Content = "555-2222" },
                new ContactInfo { Id = Guid.NewGuid(), PersonId = person2.Id, Type = ContactType.PhoneNumber, Content = "555-3333" }
            );

            await context.SaveChangesAsync();

            var controller = new LocationController(context);

            var result = await controller.Stats("Istanbul", default);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonConvert.SerializeObject(okResult.Value);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            Assert.Equal("Istanbul", data["location"].ToString());
            Assert.Equal(2, Convert.ToInt32(data["personCount"]));
            Assert.Equal(3, Convert.ToInt32(data["phoneNumberCount"]));
        }

        [Fact]
        public async Task Stats_ReturnsZeroCounts_WhenLocationIsEmptyString()
        {
            var context = GetDbContext();
            var controller = new LocationController(context);

            var result = await controller.Stats("", default);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(okResult.Value);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            Assert.Equal("", data["location"].ToString());
            Assert.Equal(0, Convert.ToInt32(data["personCount"]));
            Assert.Equal(0, Convert.ToInt32(data["phoneNumberCount"]));
        }

        [Fact]
        public async Task Stats_ReturnsZeroCounts_WhenLocationIsWhitespace()
        {
            var context = GetDbContext();
            var controller = new LocationController(context);

            var result = await controller.Stats("   ", default);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(okResult.Value);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            Assert.Equal("   ", data["location"].ToString());
            Assert.Equal(0, Convert.ToInt32(data["personCount"]));
            Assert.Equal(0, Convert.ToInt32(data["phoneNumberCount"]));
        }

        [Fact]
        public async Task Stats_ReturnsZeroCounts_WhenLocationIsNumericString()
        {
            var context = GetDbContext();
            var controller = new LocationController(context);

            var result = await controller.Stats("123", default);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(okResult.Value);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            Assert.Equal("123", data["location"].ToString());
            Assert.Equal(0, Convert.ToInt32(data["personCount"]));
            Assert.Equal(0, Convert.ToInt32(data["phoneNumberCount"]));
        }


    }
}
