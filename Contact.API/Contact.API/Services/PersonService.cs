using Contact.API.Data;
using Contact.API.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contact.API.Services
{
    public class PersonService : IPersonService
    {
        private readonly AppDbContext _context;

        public PersonService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Person>> GetAllAsync()
        {
            return await _context.Persons.Include(p => p.ContactInfos).ToListAsync();
        }

        public async Task<Person?> GetByIdAsync(Guid id)
        {
            return await _context.Persons
                .Include(p => p.ContactInfos)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Person> CreateAsync(Person person)
        {
            person.Id = Guid.NewGuid();
            _context.Persons.Add(person);
            await _context.SaveChangesAsync();
            return person;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var person = await _context.Persons
                .Include(p => p.ContactInfos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (person == null)
                return false;

            _context.ContactInfos.RemoveRange(person.ContactInfos);
            _context.Persons.Remove(person);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}