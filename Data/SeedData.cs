using BachelorRoomFinding.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BachelorRoomFinding.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

            await context.Database.MigrateAsync(); // Apply migrations

            // Seed Roles
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { RoleName = "Admin", RoleDescription = "Full Access" },
                    new Role { RoleName = "Owner", RoleDescription = "Can post rooms" },
                    new Role { RoleName = "User", RoleDescription = "Regular user" }
                );
                await context.SaveChangesAsync();
            }

            // Seed Default Admin User
            if (!context.Users.Any())
            {
                var adminRole = await context.Roles.FirstAsync(r => r.RoleName == "Admin");

                var admin = new User
                {
                    UserName = "admin",
                    Email = "admin@brf.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    RoleId = adminRole.Id,
                    Address = "Chattogram",
                    LastLogin = DateTime.Now
                };

                context.Users.Add(admin);
                await context.SaveChangesAsync();
            }
        }
    }
}