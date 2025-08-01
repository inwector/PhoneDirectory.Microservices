using Contact.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contact.API.Services
{
    public interface IPersonService
    {
        Task<List<Person>> GetAllAsync();
        Task<Person?> GetByIdAsync(Guid id);
        Task<Person> CreateAsync(Person person);
        Task<bool> DeleteAsync(Guid id);
    }
}