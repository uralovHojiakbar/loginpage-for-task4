using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using loginpage.Data;
using loginpage.Models;

namespace loginpage.Services
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> CreateAsync(User user);
        Task<IEnumerable<User>> ListSortedByLastLoginDescAsync();
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);
        Task DeleteUnverifiedAsync();
        Task BlockAsync(Guid id);
        Task UnblockAsync(Guid id);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _db;
        public UserService(AppDbContext db) => _db = db;

        public async Task<User?> GetByIdAsync(Guid id)
            => await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

        public async Task<User?> GetByEmailAsync(string email)
            => await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User> CreateAsync(User user)
        {
            // important: set ID and timestamps here
            user.Id = Guid.NewGuid();
            user.RegisteredAt = DateTime.UtcNow;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<IEnumerable<User>> ListSortedByLastLoginDescAsync()
        {
            // note: order by LastLoginAt descending, nulls last
            return await _db.Users
                .OrderByDescending(u => u.LastLoginAt ?? DateTime.MinValue)
                .ToListAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var u = await GetByIdAsync(id);
            if (u != null)
            {
                _db.Users.Remove(u);
                await _db.SaveChangesAsync();
            }
        }

        public async Task DeleteUnverifiedAsync()
        {
            var toDelete = _db.Users.Where(u => u.Status == UserStatus.Unverified);
            _db.Users.RemoveRange(toDelete);
            await _db.SaveChangesAsync();
        }

        public async Task BlockAsync(Guid id)
        {
            var u = await GetByIdAsync(id);
            if (u != null)
            {
                u.Status = UserStatus.Blocked;
                await UpdateAsync(u);
            }
        }

        public async Task UnblockAsync(Guid id)
        {
            var u = await GetByIdAsync(id);
            if (u != null)
            {
                u.Status = UserStatus.Active;
                await UpdateAsync(u);
            }
        }
    }
}