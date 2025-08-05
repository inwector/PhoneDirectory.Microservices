using Xunit;
using Moq;
using Contact.API.Controllers;
using Contact.API.Services;
using Contact.API.Data;
using Contact.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using System.Linq;

public class PersonsControllerTests
{
    private AppDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAllPersons_ReturnsOkWithPersons()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "GetAllPersonsDb")
            .Options;

        using var context = new AppDbContext(options);
        context.Persons.Add(new Person { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" });
        context.Persons.Add(new Person { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" });
        context.SaveChanges();

        var personService = new PersonService(context);
        var controller = new PersonsController(context, personService);

        var result = await controller.GetAllPersons();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var persons = Assert.IsAssignableFrom<IEnumerable<Person>>(okResult.Value);

        Assert.NotEmpty(persons);
    }

    [Fact]
    public async Task GetPerson_ReturnsPerson_WhenFound()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "GetPersonDb")
            .Options;

        var personId = Guid.NewGuid();

        using var context = new AppDbContext(options);
        context.Persons.Add(new Person { Id = personId, FirstName = "Test", LastName = "User" });
        context.SaveChanges();

        var personService = new PersonService(context);
        var controller = new PersonsController(context, personService);

        var result = await controller.GetPerson(personId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var person = Assert.IsType<Person>(okResult.Value);

        Assert.Equal(personId, person.Id);
    }

    [Fact]
    public async Task GetPerson_ReturnsNotFound_WhenNotFound()
    {
        var context = GetDbContext("GetPersonNotFoundDb");
        var personServiceMock = new Mock<IPersonService>();
        personServiceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Person)null);

        var controller = new PersonsController(context, personServiceMock.Object);

        var result = await controller.GetPerson(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreatePerson_ReturnsCreatedPerson()
    {
        var context = GetDbContext("CreatePersonDb");
        var personServiceMock = new Mock<IPersonService>();
        personServiceMock.Setup(s => s.CreateAsync(It.IsAny<Person>())).ReturnsAsync((Person p) => p);

        var controller = new PersonsController(context, personServiceMock.Object);

        var newPerson = new Person { FirstName = "New", LastName = "Person" };
        var result = await controller.CreatePerson(newPerson);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var createdPerson = Assert.IsType<Person>(createdResult.Value);
        Assert.Equal("New", createdPerson.FirstName);
    }

    [Fact]
    public async Task DeletePerson_ReturnsNoContent_WhenDeleted()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "DeletePersonDb")
            .Options;

        using var context = new AppDbContext(options);

        var personId = Guid.NewGuid();
        context.Persons.Add(new Person { Id = personId, FirstName = "Test", LastName = "User" });
        context.SaveChanges();

        var personService = new PersonService(context);
        var controller = new PersonsController(context, personService);

        var result = await controller.DeletePerson(personId);

        Assert.IsType<NoContentResult>(result);

        var deletedPerson = await context.Persons.FindAsync(personId);
        Assert.Null(deletedPerson);
    }

    [Fact]
    public async Task DeletePerson_ReturnsNotFound_WhenPersonDoesNotExist()
    {
        var context = GetDbContext("DeletePersonNotFoundDb");
        var personServiceMock = new Mock<IPersonService>();
        personServiceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var controller = new PersonsController(context, personServiceMock.Object);

        var result = await controller.DeletePerson(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RequestReport_ReturnsOk_WhenLocationExists()
    {
        var personId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        using var context = new AppDbContext(options);

        context.Persons.Add(new Person
        {
            Id = personId,
            FirstName = "Test",
            LastName = "User",
            ContactInfos = new List<ContactInfo>
        {
            new ContactInfo { Type = ContactType.Location, Content = "Ankara" }
        }
        });
        context.SaveChanges();

        var mockProducer = new Mock<IProducer<Null, string>>();
        mockProducer
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<Null, string>>(), default))
            .ReturnsAsync(new DeliveryResult<Null, string> { Status = PersistenceStatus.Persisted });

        var controller = new PersonsController(context, null); 
                                                               
        var result = await controller.RequestReport(personId, mockProducer.Object);

        
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Report request for", okResult.Value.ToString());
    }

    [Fact]
    public async Task RequestReport_ReturnsNotFound_WhenPersonNotFound()
    {
        var context = GetDbContext("RequestReportNotFoundDb");

        var mockProducer = new Mock<IProducer<Null, string>>();

        var controller = new PersonsController(context, null);
        var result = await controller.RequestReport(Guid.NewGuid(), mockProducer.Object);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task RequestReport_ReturnsBadRequest_WhenLocationMissing()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "RequestReportBadRequestDb")
            .Options;

        var personId = Guid.NewGuid();

        using var context = new AppDbContext(options);
        context.Persons.Add(new Person
        {
            Id = personId,
            FirstName = "Test",
            LastName = "User",
            ContactInfos = new List<ContactInfo>()
            
        });
        context.SaveChanges();

        var mockProducer = new Mock<IProducer<Null, string>>();
        var controller = new PersonsController(context, null);

        var result = await controller.RequestReport(personId, mockProducer.Object);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No location info found for this person.", badRequest.Value);
    }
}