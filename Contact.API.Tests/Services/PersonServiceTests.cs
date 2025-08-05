using Contact.API.Data;
using Contact.API.Models;
using Contact.API.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Contact.API.Tests.Services
{
    public class PersonServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private PersonService GetService(AppDbContext context)
        {
            return new PersonService(context);
        }

        [Fact]
        public async Task CreateAsync_Should_AddPerson_And_ReturnWithId()
        {
            var context = GetDbContext();
            var service = GetService(context);

            var person = new Person
            {
                FirstName = "John",
                LastName = "Doe",
                Company = "Acme Corp"
            };

            var result = await service.CreateAsync(person);

            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Single(context.Persons);
        }

        [Fact]
        public async Task GetAllAsync_Should_ReturnAllPersons()
        {
            var context = GetDbContext();
            context.Persons.AddRange(
                new Person { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Company = "C" },
                new Person { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y", Company = "Z" }
            );
            await context.SaveChangesAsync();

            var service = GetService(context);
            var result = await service.GetAllAsync();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByIdAsync_Should_ReturnPersonWithContactInfos()
        {
            var context = GetDbContext();
            var personId = Guid.NewGuid();
            context.Persons.Add(new Person
            {
                Id = personId,
                FirstName = "John",
                LastName = "Doe",
                Company = "CompanyX",
                ContactInfos = new List<ContactInfo>
                {
                    new ContactInfo { Id = Guid.NewGuid(), Type = ContactType.EmailAddress, Content = "test@example.com" }
                }
            });
            await context.SaveChangesAsync();

            var service = GetService(context);
            var result = await service.GetByIdAsync(personId);

            Assert.NotNull(result);
            Assert.Equal("John", result.FirstName);
            Assert.Single(result.ContactInfos);
        }

        [Fact]
        public async Task DeleteAsync_Should_RemovePersonAndContactInfos_WhenExists()
        {
            var context = GetDbContext();
            var personId = Guid.NewGuid();
            context.Persons.Add(new Person
            {
                Id = personId,
                FirstName = "A",
                LastName = "B",
                Company = "C",
                ContactInfos = new List<ContactInfo>
                {
                    new ContactInfo { Id = Guid.NewGuid(), Type = ContactType.PhoneNumber, Content = "123" }
                }
            });
            await context.SaveChangesAsync();

            var service = GetService(context);
            var result = await service.DeleteAsync(personId);

            Assert.True(result);
            Assert.Empty(context.Persons);
            Assert.Empty(context.ContactInfos);
        }

        [Fact]
        public async Task DeleteAsync_Should_ReturnFalse_WhenPersonDoesNotExist()
        {
            var context = GetDbContext();
            var service = GetService(context);

            var result = await service.DeleteAsync(Guid.NewGuid());

            Assert.False(result);
        }
    }
}
