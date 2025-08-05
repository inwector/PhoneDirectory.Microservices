using Contact.API.Controllers;
using Contact.API.Data;
using Contact.API.DTOs;
using Contact.API.Models;
using Contact.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Contact.API.Tests.Controllers
{
    public class PersonsControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private Mock<IPersonService> GetPersonServiceMock(AppDbContext context)
        {
            var mock = new Mock<IPersonService>();

            mock.Setup(s => s.GetAllAsync())
                .ReturnsAsync(() => context.Persons.Include(p => p.ContactInfos).ToList());

            mock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => context.Persons.Include(p => p.ContactInfos).FirstOrDefault(p => p.Id == id));

            mock.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var person = context.Persons.Include(p => p.ContactInfos).FirstOrDefault(p => p.Id == id);
                    if (person == null)
                        return false;
                    context.ContactInfos.RemoveRange(person.ContactInfos);
                    context.Persons.Remove(person);
                    context.SaveChanges();
                    return true;
                });

            return mock;
        }

        [Fact]
        public async Task GetAllPersons_ReturnsAllPersons()
        {
            var context = GetDbContext();
            var person1 = new Person { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Company = "X" };
            var person2 = new Person { Id = Guid.NewGuid(), FirstName = "C", LastName = "D", Company = "Y" };
            context.Persons.AddRange(person1, person2);
            await context.SaveChangesAsync();

            var serviceMock = GetPersonServiceMock(context);
            var controller = new PersonsController(context, serviceMock.Object);

            var result = await controller.GetAllPersons();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var persons = Assert.IsAssignableFrom<IEnumerable<Person>>(okResult.Value);
            Assert.Equal(2, persons.Count());
        }

        [Fact]
        public async Task GetPerson_ReturnsPersonWithContactInfos_WhenExists()
        {
            var context = GetDbContext();
            var person = new Person
            {
                Id = Guid.NewGuid(),
                FirstName = "First",
                LastName = "Last",
                Company = "Company",
                ContactInfos = new List<ContactInfo>
        {
            new ContactInfo { Id = Guid.NewGuid(), Type = Models.ContactType.EmailAddress, Content = "email@test.com" }
        }
            };
            context.Persons.Add(person);
            await context.SaveChangesAsync();

            var serviceMock = GetPersonServiceMock(context);
            var controller = new PersonsController(context, serviceMock.Object);

            var result = await controller.GetPerson(person.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(okResult.Value);
            var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            var idString = dict["Id"].ToString();
            Assert.Equal(person.Id, Guid.Parse(idString));

            var contactInfosJson = dict["ContactInfos"].ToString();
            var contactInfos = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(contactInfosJson);

            Assert.Single(contactInfos);
            Assert.Equal("EmailAddress", contactInfos[0]["Type"].ToString());
            Assert.Equal("email@test.com", contactInfos[0]["Content"].ToString());
        }

        [Fact]
        public async Task GetPerson_ReturnsNotFound_WhenPersonDoesNotExist()
        {
            var context = GetDbContext();
            var serviceMock = GetPersonServiceMock(context);
            var controller = new PersonsController(context, serviceMock.Object);

            var result = await controller.GetPerson(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreatePerson_CreatesPersonAndReturnsId()
        {
            var context = GetDbContext();
            var serviceMock = GetPersonServiceMock(context);
            var controller = new PersonsController(context, serviceMock.Object);

            var dto = new PersonCreateDto
            {
                FirstName = "John",
                LastName = "Doe",
                Company = "Company",
                ContactInfos = new List<ContactInfoCreateDto>
                {
                    new ContactInfoCreateDto { Content = "12345", Type = (int)Models.ContactType.PhoneNumber }
                }
            };

            var result = await controller.CreatePerson(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(okResult.Value);
            var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            var personIdString = dict["personid"].ToString();
            Guid personId = Guid.Parse(personIdString);

            var personInDb = await context.Persons
                                         .Include(p => p.ContactInfos)
                                         .FirstOrDefaultAsync(p => p.Id == personId);
            Assert.NotNull(personInDb);
            Assert.Equal("John", personInDb.FirstName);
            Assert.Single(personInDb.ContactInfos);
        }

        [Fact]
        public async Task DeletePerson_ReturnsOk_WhenPersonExists()
        {
            var context = GetDbContext();
            var person = new Person { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Company = "C" };
            context.Persons.Add(person);
            await context.SaveChangesAsync();

            var serviceMock = GetPersonServiceMock(context);
            var controller = new PersonsController(context, serviceMock.Object);

            var result = await controller.DeletePerson(person.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("was deleted", okResult.Value.ToString());
            Assert.Null(await context.Persons.FindAsync(person.Id));
        }

        [Fact]
        public async Task DeletePerson_ReturnsNotFound_WhenPersonDoesNotExist()
        {
            var context = GetDbContext();
            var serviceMock = GetPersonServiceMock(context);
            var controller = new PersonsController(context, serviceMock.Object);

            var result = await controller.DeletePerson(Guid.NewGuid());

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("doesn't exist", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task RequestReport_ReturnsOk_WhenPersonAndLocationExist()
        {
            var context = GetDbContext();
            var person = new Person
            {
                Id = Guid.NewGuid(),
                FirstName = "A",
                LastName = "B",
                Company = "C",
                ContactInfos = new List<ContactInfo>
        {
            new ContactInfo
            {
                Id = Guid.NewGuid(),
                Type = Models.ContactType.Location,
                Content = "Istanbul"
            }
        }
            };
            context.Persons.Add(person);
            await context.SaveChangesAsync();

            var mockProducer = new Mock<IProducer<Null, string>>();
            mockProducer
                .Setup(p => p.ProduceAsync(
                    It.IsAny<string>(),
                    It.IsAny<Message<Null, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeliveryResult<Null, string>
                {
                    Topic = "report-requests",
                    Partition = new Partition(0),
                    Offset = new Offset(0),
                    Message = new Message<Null, string> { Value = "Report for Istanbul" }
                });

            var serviceMock = GetPersonServiceMock(context);
            var controller = new PersonsController(context, serviceMock.Object);

            var result = await controller.RequestReport(person.Id, mockProducer.Object);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var message = okResult.Value?.GetType().GetProperty("message")?.GetValue(okResult.Value, null)?.ToString();
            Assert.Contains("Report request for", message);

            mockProducer.Verify(p => p.ProduceAsync(
                "report-requests",
                It.Is<Message<Null, string>>(m => m.Value.Contains("Istanbul")),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }


        [Fact]
        public async Task RequestReport_ReturnsNotFound_WhenPersonDoesNotExist()
        {
            var context = GetDbContext();
            var mockProducer = new Mock<IProducer<Null, string>>();
            var serviceMock = GetPersonServiceMock(context);
            var controller = new PersonsController(context, serviceMock.Object);

            var result = await controller.RequestReport(Guid.NewGuid(), mockProducer.Object);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task RequestReport_ReturnsBadRequest_WhenNoLocationInfo()
        {
            var context = GetDbContext();
            var person = new Person
            {
                Id = Guid.NewGuid(),
                FirstName = "A",
                LastName = "B",
                Company = "C",
                ContactInfos = new List<ContactInfo>()
            };
            context.Persons.Add(person);
            await context.SaveChangesAsync();

            var mockProducer = new Mock<IProducer<Null, string>>();
            var serviceMock = GetPersonServiceMock(context);
            var controller = new PersonsController(context, serviceMock.Object);

            var result = await controller.RequestReport(person.Id, mockProducer.Object);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("No location info", badRequestResult.Value.ToString());
        }
    }
}
