using System.Security.Cryptography;
using System.Text;
using HealthCheckApi.Models;
using HealthCheckApi.DTOs;
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

        public async Task<User?> CreateUserAsync(CreateUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return null;

            if(dto.Roles == null || !dto.Roles.Any())
                dto = dto with {Roles = new List<string> {"User"} };

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password),
                Roles = dto.Roles,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
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
            
            
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }
       
    }
}