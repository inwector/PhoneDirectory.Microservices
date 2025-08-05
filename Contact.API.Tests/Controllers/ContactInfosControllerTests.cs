using Contact.API.Controllers;
using Contact.API.Data;
using Contact.API.DTOs;
using Contact.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Contact.API.Tests.Controllers
{
    public class ContactInfosControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task AddContactInfo_ReturnsCreated_WhenPersonExists()
        {
            var context = GetDbContext();
            var person = new Person
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Company = "TestCorp"
            };
            context.Persons.Add(person);
            await context.SaveChangesAsync();

            var controller = new ContactInfosController(context);
            var dto = new ContactAddDto
            {
                Type = "PhoneNumber",
                Content = "123-456"
            };

            var result = await controller.AddContactInfo(person.Id, dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var contactInfo = Assert.IsType<ContactInfo>(createdResult.Value);
            Assert.Equal("123-456", contactInfo.Content);
            Assert.Equal(Models.ContactType.PhoneNumber, contactInfo.Type);
            Assert.Equal(person.Id, contactInfo.PersonId);
        }

        [Fact]
        public async Task AddContactInfo_ReturnsNotFound_WhenPersonDoesNotExist()
        {
            var context = GetDbContext();
            var controller = new ContactInfosController(context);
            var dto = new ContactAddDto
            {
                Type = "EmailAddress",
                Content = "john@example.com"
            };

            var result = await controller.AddContactInfo(Guid.NewGuid(), dto);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFound.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetContactInfo_ReturnsOk_WhenExists()
        {
            var context = GetDbContext();
            var person = new Person { Id = Guid.NewGuid(), FirstName = "Ali", LastName = "Veli" };
            var contact = new ContactInfo
            {
                Id = Guid.NewGuid(),
                PersonId = person.Id,
                Type = Models.ContactType.Location,
                Content = "Ankara"
            };
            context.Persons.Add(person);
            context.ContactInfos.Add(contact);
            await context.SaveChangesAsync();

            var controller = new ContactInfosController(context);

            var result = await controller.GetContactInfo(person.Id, contact.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ContactInfo>(ok.Value);
            Assert.Equal(contact.Id, returned.Id);
        }

        [Fact]
        public async Task GetContactInfo_ReturnsNotFound_WhenContactDoesNotExist()
        {
            var context = GetDbContext();

            var person = new Person { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User" };
            context.Persons.Add(person);
            await context.SaveChangesAsync();

            var controller = new ContactInfosController(context);
            var nonExistingContactId = Guid.NewGuid();

            var result = await controller.GetContactInfo(person.Id, nonExistingContactId);

            var notFoundResult = Assert.IsType<NotFoundResult>(result);
        }


        [Fact]
        public async Task DeleteContactInfo_ReturnsOk_WhenExists()
        {
            var context = GetDbContext();
            var person = new Person { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User" };
            var contact = new ContactInfo
            {
                Id = Guid.NewGuid(),
                PersonId = person.Id,
                Type = Models.ContactType.EmailAddress,
                Content = "mail@test.com"
            };
            context.Persons.Add(person);
            context.ContactInfos.Add(contact);
            await context.SaveChangesAsync();

            var controller = new ContactInfosController(context);

            var result = await controller.DeleteContactInfo(person.Id, contact.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("was deleted", ok.Value.ToString());
        }

        [Fact]
        public async Task DeleteContactInfo_ReturnsNotFound_WhenContactDoesNotExist()
        {
            var context = GetDbContext();
            var person = new Person { Id = Guid.NewGuid(), FirstName = "Ghost", LastName = "User" };
            context.Persons.Add(person);
            await context.SaveChangesAsync();

            var controller = new ContactInfosController(context);

            var result = await controller.DeleteContactInfo(person.Id, Guid.NewGuid());

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No record", notFound.Value.ToString());
        }

    }
}
