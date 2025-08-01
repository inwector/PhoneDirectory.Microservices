using Contact.API.Data;
using Contact.API.Models;
using Contact.API.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Contact.API.Tests.Services
{
    public class PersonServiceTests
    {
        private readonly AppDbContext _context;
        private readonly PersonService _service;

        public PersonServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Her test için temiz DB
                .Options;

            _context = new AppDbContext(options);
            _service = new PersonService(_context);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddPerson()
        {
            var person = new Person
            {
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                Company = "Test Co"
            };

            var createdPerson = await _service.CreateAsync(person);

            Assert.NotNull(createdPerson);
            Assert.NotEqual(Guid.Empty, createdPerson.Id);

            var dbPerson = await _context.Persons.FindAsync(createdPerson.Id);
            Assert.NotNull(dbPerson);
            Assert.Equal("Ahmet", dbPerson.FirstName);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnPerson_WhenExists()
        {
            var person = new Person { FirstName = "Mehmet", LastName = "Kara", Company = "ABC" };
            var created = await _service.CreateAsync(person);

            var fetched = await _service.GetByIdAsync(created.Id);

            Assert.NotNull(fetched);
            Assert.Equal("Mehmet", fetched.FirstName);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            var fetched = await _service.GetByIdAsync(Guid.NewGuid());
            Assert.Null(fetched);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenPersonExists()
        {
            var person = new Person { FirstName = "Selin", LastName = "Güneş" };
            var created = await _service.CreateAsync(person);

            var result = await _service.DeleteAsync(created.Id);

            Assert.True(result);
            var dbPerson = await _context.Persons.FindAsync(created.Id);
            Assert.Null(dbPerson);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenPersonDoesNotExist()
        {
            var result = await _service.DeleteAsync(Guid.NewGuid());
            Assert.False(result);
        }
    }
}