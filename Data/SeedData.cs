using BachelorRoomFinding.Entities;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

            await context.Database.MigrateAsync();

            // ── Seed Roles ──────────────────────────────────────────────
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { RoleName = "Admin",  RoleDescription = "Full system access" },
                    new Role { RoleName = "Owner",  RoleDescription = "Can post and manage rooms" },
                    new Role { RoleName = "User",   RoleDescription = "Can browse and apply for rooms" }
                );
                await context.SaveChangesAsync();
            }

            // ── Seed Admin ───────────────────────────────────────────────
            if (!context.Users.Any(u => u.Email == "admin@brf.com"))
            {
                var adminRole = await context.Roles.FirstAsync(r => r.RoleName == "Admin");
                context.Users.Add(new User
                {
                    UserName         = "admin",
                    Email            = "admin@brf.com",
                    PasswordHash     = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    RoleId           = adminRole.Id,
                    Address          = "Chattogram",
                    PhoneNumber      = "+8801700000000",
                    IsApprovedByAdmin= true,
                    IsVerified       = true,
                    IsEmailVerified  = true,
                    AccountStatus    = AccountStatus.Active,
                    CreatedAt        = DateTime.Now,
                    LastLogin        = DateTime.Now
                });
                await context.SaveChangesAsync();
            }

            // ── Seed Demo Owner ──────────────────────────────────────────
            if (!context.Users.Any(u => u.Email == "owner@brf.com"))
            {
                var ownerRole = await context.Roles.FirstAsync(r => r.RoleName == "Owner");
                context.Users.Add(new User
                {
                    UserName         = "demoowner",
                    Email            = "owner@brf.com",
                    PasswordHash     = BCrypt.Net.BCrypt.HashPassword("owner123"),
                    RoleId           = ownerRole.Id,
                    Address          = "Dhaka",
                    PhoneNumber      = "+8801811111111",
                    IsApprovedByAdmin= true,
                    IsVerified       = true,
                    IsEmailVerified  = true,
                    AccountStatus    = AccountStatus.Active,
                    CreatedAt        = DateTime.Now
                });
                await context.SaveChangesAsync();
            }

            // ── Seed Demo User ───────────────────────────────────────────
            if (!context.Users.Any(u => u.Email == "user@brf.com"))
            {
                var userRole = await context.Roles.FirstAsync(r => r.RoleName == "User");
                context.Users.Add(new User
                {
                    UserName         = "demouser",
                    Email            = "user@brf.com",
                    PasswordHash     = BCrypt.Net.BCrypt.HashPassword("user123"),
                    RoleId           = userRole.Id,
                    Address          = "Sylhet",
                    PhoneNumber      = "+8801922222222",
                    IsApprovedByAdmin= true,
                    IsVerified       = false,
                    IsEmailVerified  = true,
                    AccountStatus    = AccountStatus.Active,
                    CreatedAt        = DateTime.Now
                });
                await context.SaveChangesAsync();
            }
        }
    }
}