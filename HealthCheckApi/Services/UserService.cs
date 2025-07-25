using System.Security.Cryptography;
using System.Text;
using HealthCheckApi.Models;
using HealthCheckApi.Data;
using Microsoft.EntityFrameworkCore;

namespace  HealthCheckApi.Services
{
    public class UserService
    {
        private readonly ItemsDbContext _context;

        public UserService(ItemsDbContext context)
        {
            _context = context;
        }

        //password hashing
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public async Task<User?> CreateUserAsync(string name, string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            return null;

            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = HashPassword(password),
                Roles = new List<string> { "User"},
                CreatedAt = DateTime.UtcNow,
                isActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if(user == null || !VerifyPassword(password, user.PasswordHash))
                return null;
            
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return user;
        }
       
    }
}