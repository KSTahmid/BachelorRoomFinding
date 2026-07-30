using BachelorRoomFinding.Entities;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Data
{
    public static class SeedData
    {
        // Real Unsplash photos grouped by room type — curated to match
        // Bangladeshi bachelor mess / hostel aesthetics (small rooms, fans, shared areas)
        private static readonly string[][] RoomPhotoSets = new[]
        {
            // Set 0 — Small single bedroom with fan
            new[]
            {
                "https://images.unsplash.com/photo-1555854877-bab0e564b8d5?w=900&q=80",
                "https://images.unsplash.com/photo-1505693314120-0d443867891c?w=900&q=80",
                "https://images.unsplash.com/photo-1484101403633-562f891dc89a?w=900&q=80"
            },
            // Set 1 — Modest furnished room / hostel style
            new[]
            {
                "https://images.unsplash.com/photo-1536376072261-38c75010e6c9?w=900&q=80",
                "https://images.unsplash.com/photo-1560185893-a55cbc8c57e8?w=900&q=80",
                "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?w=900&q=80"
            },
            // Set 2 — Simple apartment / sublet style
            new[]
            {
                "https://images.unsplash.com/photo-1493809842364-78817add7ffb?w=900&q=80",
                "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=900&q=80",
                "https://images.unsplash.com/photo-1574362848149-11496d93a7c7?w=900&q=80"
            },
            // Set 3 — Shared hostel / seat style
            new[]
            {
                "https://images.unsplash.com/photo-1540518614846-7eded433c457?w=900&q=80",
                "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=900&q=80",
                "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=900&q=80"
            },
            // Set 4 — Bachelor flat / shared kitchen
            new[]
            {
                "https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=900&q=80",
                "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?w=900&q=80",
                "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=900&q=80"
            },
            // Set 5 — Budget room with window view
            new[]
            {
                "https://images.unsplash.com/photo-1598928506311-c55ded91a20c?w=900&q=80",
                "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=900&q=80",
                "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=900&q=80"
            },
            // Set 6 — Compact room, desk & bed
            new[]
            {
                "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=900&q=80",
                "https://images.unsplash.com/photo-1587985064135-0366536eab42?w=900&q=80",
                "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=900&q=80"
            },
            // Set 7 — Shared bathroom / building exterior
            new[]
            {
                "https://images.unsplash.com/photo-1552321554-5fefe8c9ef14?w=900&q=80",
                "https://images.unsplash.com/photo-1564540583246-934409427776?w=900&q=80",
                "https://images.unsplash.com/photo-1556909172-54557c7e4fb7?w=900&q=80"
            },
        };

        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new AppDbContext(serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());
            if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.MigrateAsync();
                await EnsurePresentationSchemaAsync(context);
            }

            // === DANGER: RESET DATABASE ===
            // Uncomment the lines below ONLY if you want to completely wipe the database 
            // and start fresh with the new seed data.
            /*
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
            */
            // End of Reset block

            // Idempotency guard: If this user exists, seeding has already been done.
            if (await context.Users.AnyAsync(u => u.Email == "raktimwhattsapp@gmail.com"))
            {
                return; // Data already seeded
            }

            // 1. Seed Roles
            var adminRole = new Role { RoleName = "Admin", RoleDescription = "Full system access" };
            var ownerRole = new Role { RoleName = "Owner", RoleDescription = "Can post and manage rooms" };
            var userRole = new Role { RoleName = "User", RoleDescription = "Can browse and apply for rooms" };

            if (!context.Roles.Any())
            {
                context.Roles.AddRange(adminRole, ownerRole, userRole);
                await context.SaveChangesAsync();
            }
            else
            {
                adminRole = await context.Roles.FirstAsync(r => r.RoleName == "Admin");
                ownerRole = await context.Roles.FirstAsync(r => r.RoleName == "Owner");
                userRole = await context.Roles.FirstAsync(r => r.RoleName == "User");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

            // 2. Seed Admins (2)
            var admins = new List<User>
            {
                new User { UserName = "Super Admin", Email = "admin1@brf.com", PasswordHash = passwordHash, RoleId = adminRole.Id, Address = "Dhaka", PhoneNumber = "01700000001", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now },
                new User { UserName = "Support Admin", Email = "admin2@brf.com", PasswordHash = passwordHash, RoleId = adminRole.Id, Address = "Dhaka", PhoneNumber = "01700000002", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now }
            };

            // 3. Seed Owners (5) including raktim305@gmail.com
            var owners = new List<User>
            {
                new User { UserName = "Raktim Owner", Email = "raktim305@gmail.com", PasswordHash = passwordHash, RoleId = ownerRole.Id, Address = "Dhanmondi, Dhaka", PhoneNumber = "01700000003", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now },
                new User { UserName = "Karim Hossain", Email = "owner2@brf.com", PasswordHash = passwordHash, RoleId = ownerRole.Id, Address = "Mirpur, Dhaka", PhoneNumber = "01700000004", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now },
                new User { UserName = "Shafiqur Rahman", Email = "owner3@brf.com", PasswordHash = passwordHash, RoleId = ownerRole.Id, Address = "Uttara, Dhaka", PhoneNumber = "01700000005", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now },
                new User { UserName = "Jalal Ahmed", Email = "owner4@brf.com", PasswordHash = passwordHash, RoleId = ownerRole.Id, Address = "GEC, Chattogram", PhoneNumber = "01700000006", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now },
                new User { UserName = "Faisal", Email = "owner5@brf.com", PasswordHash = passwordHash, RoleId = ownerRole.Id, Address = "Khulshi, Chattogram", PhoneNumber = "01700000007", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now }
            };

            // 4. Seed Users (5) including raktimwhattsapp@gmail.com
            var users = new List<User>
            {
                new User { UserName = "Raktim Tenant", Email = "raktimwhattsapp@gmail.com", PasswordHash = passwordHash, RoleId = userRole.Id, Address = "Mohammadpur, Dhaka", PhoneNumber = "01700000008", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now },
                new User { UserName = "Tahmid", Email = "user2@brf.com", PasswordHash = passwordHash, RoleId = userRole.Id, Address = "Badda, Dhaka", PhoneNumber = "01700000009", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now },
                new User { UserName = "Sabbir", Email = "user3@brf.com", PasswordHash = passwordHash, RoleId = userRole.Id, Address = "Farmgate, Dhaka", PhoneNumber = "01700000010", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now },
                new User { UserName = "Nabil", Email = "user4@brf.com", PasswordHash = passwordHash, RoleId = userRole.Id, Address = "Agrabad, Chattogram", PhoneNumber = "01700000011", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now },
                new User { UserName = "Sakib", Email = "user5@brf.com", PasswordHash = passwordHash, RoleId = userRole.Id, Address = "Muradpur, Chattogram", PhoneNumber = "01700000012", IsApprovedByAdmin = true, IsVerified = true, IsEmailVerified = true, AccountStatus = AccountStatus.Active, CreatedAt = DateTime.Now }
            };

            context.Users.AddRange(admins);
            context.Users.AddRange(owners);
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            // 5. Seed 10 Hardcoded Rooms
            var rooms = new List<Room>
            {
                // Dhaka Rooms (6)
                new Room { Title = "Premium Bachelor Flat in Dhanmondi", Description = "Fully furnished bachelor flat with AC and WiFi. Only for executives.", Address = "House 12, Road 4, Dhanmondi", District = "Dhaka", Thana = "Dhanmondi", MonthlyRent = 15000, SeatRent = 0, ElectricityBill = 1000, WiFiBill = 500, GasBill = 1080, WaterBill = 300, ServiceCharge = 1000, MealCost = 4000, SecurityDeposit = 15000, Advance = 15000, BedroomCount = 2, RoomType = RoomType.BachelorFlat, Status = RoomStatus.Active, OwnerId = owners[0].UserId, PostedDate = DateTime.Now.AddDays(-2), Rules = "Bachelor Only|No Smoking" },
                new Room { Title = "Shared Seat in Mirpur 10", Description = "1 seat available in a 3-bed room. Good environment.", Address = "House 45, Block C, Mirpur 10", District = "Dhaka", Thana = "Mirpur", MonthlyRent = 4000, SeatRent = 4000, ElectricityBill = 500, WiFiBill = 200, GasBill = 1080, WaterBill = 200, ServiceCharge = 500, MealCost = 3500, SecurityDeposit = 4000, Advance = 4000, BedroomCount = 3, RoomType = RoomType.SharedSeat, Status = RoomStatus.Active, OwnerId = owners[1].UserId, PostedDate = DateTime.Now.AddDays(-5), Rules = "Bachelor Only|Guest Allowed" },
                new Room { Title = "Female Hostel Seat in Uttara", Description = "Secure female hostel with dining facility.", Address = "Sector 7, Road 14, Uttara", District = "Dhaka", Thana = "Uttara", MonthlyRent = 6500, SeatRent = 6500, ElectricityBill = 800, WiFiBill = 300, GasBill = 1080, WaterBill = 400, ServiceCharge = 800, MealCost = 4500, SecurityDeposit = 6500, Advance = 6500, BedroomCount = 4, RoomType = RoomType.FemaleHostel, Status = RoomStatus.Active, OwnerId = owners[2].UserId, PostedDate = DateTime.Now.AddDays(-10), Rules = "Curfew: 9:00 PM|No Smoking" },
                new Room { Title = "Male Mess in Farmgate", Description = "Single room available in a male mess. Walking distance to university.", Address = "Tejkunipara, Farmgate", District = "Dhaka", Thana = "Farmgate", MonthlyRent = 7000, SeatRent = 0, ElectricityBill = 600, WiFiBill = 300, GasBill = 1080, WaterBill = 300, ServiceCharge = 600, MealCost = 3500, SecurityDeposit = 7000, Advance = 7000, BedroomCount = 1, RoomType = RoomType.MaleMess, Status = RoomStatus.Active, OwnerId = owners[0].UserId, PostedDate = DateTime.Now.AddDays(-1), Rules = "Bachelor Only" },
                new Room { Title = "Sublet Room in Bashundhara", Description = "Small sublet room for a student or bachelor.", Address = "Block D, Bashundhara R/A", District = "Dhaka", Thana = "Bashundhara", MonthlyRent = 8000, SeatRent = 0, ElectricityBill = 700, WiFiBill = 400, GasBill = 1080, WaterBill = 300, ServiceCharge = 1000, MealCost = 4000, SecurityDeposit = 8000, Advance = 8000, BedroomCount = 1, RoomType = RoomType.Sublet, Status = RoomStatus.Active, OwnerId = owners[1].UserId, PostedDate = DateTime.Now.AddDays(-15), Rules = "No Guests|No Smoking" },
                new Room { Title = "Full Apartment in Badda", Description = "2 bedroom apartment perfect for a small group of bachelors.", Address = "Middle Badda", District = "Dhaka", Thana = "Badda", MonthlyRent = 12000, SeatRent = 0, ElectricityBill = 1200, WiFiBill = 500, GasBill = 1080, WaterBill = 400, ServiceCharge = 1500, MealCost = 0, SecurityDeposit = 12000, Advance = 12000, BedroomCount = 2, RoomType = RoomType.BachelorFlat, Status = RoomStatus.Active, OwnerId = owners[2].UserId, PostedDate = DateTime.Now.AddDays(-4), Rules = "Bachelor Only" },
                
                // Chattogram Rooms (4)
                new Room { Title = "Bachelor Flat near GEC", Description = "Spacious flat near GEC intersection.", Address = "O.R. Nizam Road, GEC", District = "Chattogram", Thana = "GEC", MonthlyRent = 10000, SeatRent = 0, ElectricityBill = 800, WiFiBill = 400, GasBill = 1080, WaterBill = 300, ServiceCharge = 800, MealCost = 3500, SecurityDeposit = 10000, Advance = 10000, BedroomCount = 2, RoomType = RoomType.BachelorFlat, Status = RoomStatus.Active, OwnerId = owners[3].UserId, PostedDate = DateTime.Now.AddDays(-6), Rules = "Bachelor Only" },
                new Room { Title = "Shared Seat in Khulshi", Description = "Quiet environment, 1 seat available.", Address = "South Khulshi", District = "Chattogram", Thana = "Khulshi", MonthlyRent = 3500, SeatRent = 3500, ElectricityBill = 400, WiFiBill = 200, GasBill = 1080, WaterBill = 200, ServiceCharge = 500, MealCost = 3000, SecurityDeposit = 3500, Advance = 3500, BedroomCount = 1, RoomType = RoomType.SharedSeat, Status = RoomStatus.Active, OwnerId = owners[4].UserId, PostedDate = DateTime.Now.AddDays(-8), Rules = "Guest Allowed" },
                new Room { Title = "Male Mess near Nasirabad", Description = "Mess suitable for university students.", Address = "Nasirabad Housing Society", District = "Chattogram", Thana = "Nasirabad", MonthlyRent = 5000, SeatRent = 0, ElectricityBill = 500, WiFiBill = 300, GasBill = 1080, WaterBill = 250, ServiceCharge = 600, MealCost = 3000, SecurityDeposit = 5000, Advance = 5000, BedroomCount = 1, RoomType = RoomType.MaleMess, Status = RoomStatus.Active, OwnerId = owners[3].UserId, PostedDate = DateTime.Now.AddDays(-12), Rules = "Bachelor Only" },
                new Room { Title = "Female Hostel in Chawkbazar", Description = "Secure environment for female students.", Address = "Chawkbazar", District = "Chattogram", Thana = "Chawkbazar", MonthlyRent = 4500, SeatRent = 4500, ElectricityBill = 600, WiFiBill = 300, GasBill = 1080, WaterBill = 300, ServiceCharge = 700, MealCost = 3500, SecurityDeposit = 4500, Advance = 4500, BedroomCount = 3, RoomType = RoomType.FemaleHostel, Status = RoomStatus.Active, OwnerId = owners[4].UserId, PostedDate = DateTime.Now.AddDays(-3), Rules = "Curfew: 9:00 PM" }
            };

            context.Rooms.AddRange(rooms);
            await context.SaveChangesAsync();

            // 6. Add Photos & Facilities
            var facilities = new[] { "WiFi", "Attached Bathroom", "Fan", "Drinking Water" };
            foreach (var room in rooms)
            {
                int setIndex = ((int)room.RoomType) % RoomPhotoSets.Length;
                var photoSet = RoomPhotoSets[setIndex];
                bool firstPhoto = true;
                foreach (var photoUrl in photoSet)
                {
                    context.RoomPhotos.Add(new RoomPhoto { RoomId = room.Id, PhotoPath = photoUrl, IsPrimary = firstPhoto, IsVideo = false });
                    firstPhoto = false;
                }
                foreach (var fac in facilities)
                {
                    context.RoomFacilities.Add(new RoomFacility { RoomId = room.Id, FacilityName = fac });
                }
            }
            await context.SaveChangesAsync();

            // 7. Seed 3 Rental Applications for Raktim Owner (owners[0]) and Raktim Tenant (users[0])
            var room1 = rooms[0]; // Premium Bachelor Flat (Owned by Raktim Owner)
            var room2 = rooms[3]; // Male Mess in Farmgate (Owned by Raktim Owner)
            var room3 = rooms[1]; // Shared Seat in Mirpur 10 (Owned by Karim Hossain)

            var applications = new List<RentalApplication>
            {
                new RentalApplication { RoomId = room1.Id, ApplicantId = users[0].UserId, MoveInDate = DateTime.Now.AddDays(5), DurationMonths = 12, Status = ApplicationStatus.Pending, AppliedAt = DateTime.Now.AddDays(-1) },
                new RentalApplication { RoomId = room2.Id, ApplicantId = users[1].UserId, MoveInDate = DateTime.Now.AddDays(10), DurationMonths = 6, Status = ApplicationStatus.Approved, AppliedAt = DateTime.Now.AddDays(-2) },
                new RentalApplication { RoomId = room3.Id, ApplicantId = users[0].UserId, MoveInDate = DateTime.Now.AddDays(15), DurationMonths = 6, Status = ApplicationStatus.Rejected, AppliedAt = DateTime.Now.AddDays(-3) }
            };

            context.RentalApplications.AddRange(applications);
            await context.SaveChangesAsync();
            
            // Add a pending payment for the approved application
            var payment = new Payment
            {
                ApplicationId = applications[1].Id,
                UserId = users[1].UserId,
                Method = "bKash",
                Amount = room2.Advance,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            context.Payments.Add(payment);
            await context.SaveChangesAsync();
        }

        private static async Task EnsurePresentationSchemaAsync(AppDbContext context)
        {
            var sqlStatements = new[]
            {
                "IF COL_LENGTH('Users', 'BkashNumber') IS NULL ALTER TABLE [Users] ADD [BkashNumber] nvarchar(max) NULL;",
                "IF COL_LENGTH('Users', 'NagadNumber') IS NULL ALTER TABLE [Users] ADD [NagadNumber] nvarchar(max) NULL;",
                "IF COL_LENGTH('Users', 'IsDemoNumber') IS NULL ALTER TABLE [Users] ADD [IsDemoNumber] bit NOT NULL CONSTRAINT [DF_Users_IsDemoNumber] DEFAULT CAST(1 AS bit);",
                "IF COL_LENGTH('RoommateConnectionRequests', 'RespondedAt') IS NULL ALTER TABLE [RoommateConnectionRequests] ADD [RespondedAt] datetime2 NULL;",
                "IF COL_LENGTH('Payments', 'OwnerId') IS NULL ALTER TABLE [Payments] ADD [OwnerId] int NULL;",
                "IF COL_LENGTH('Payments', 'RoomId') IS NULL ALTER TABLE [Payments] ADD [RoomId] int NULL;",
                "IF COL_LENGTH('Payments', 'SenderWalletNumber') IS NULL ALTER TABLE [Payments] ADD [SenderWalletNumber] nvarchar(max) NULL;",
                "IF COL_LENGTH('Payments', 'RecipientWalletNumber') IS NULL ALTER TABLE [Payments] ADD [RecipientWalletNumber] nvarchar(max) NULL;",
                "IF COL_LENGTH('MessExpenses', 'ReceiptImagePath') IS NULL ALTER TABLE [MessExpenses] ADD [ReceiptImagePath] nvarchar(max) NULL;",
                "IF COL_LENGTH('MessGroups', 'InviteCode') IS NULL ALTER TABLE [MessGroups] ADD [InviteCode] nvarchar(16) NULL;",
                "UPDATE [MessGroups] SET [InviteCode] = CONCAT('MB', RIGHT('000000' + CAST([Id] AS varchar(6)), 6)) WHERE [InviteCode] IS NULL;",
                "IF COL_LENGTH('MessMembers', 'Role') IS NULL ALTER TABLE [MessMembers] ADD [Role] int NOT NULL CONSTRAINT [DF_MessMembers_Role] DEFAULT 2;",
                @"IF OBJECT_ID('MessFundEntries', 'U') IS NULL
                  CREATE TABLE [MessFundEntries] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_MessFundEntries] PRIMARY KEY,
                    [MessGroupId] int NOT NULL,
                    [UserId] int NOT NULL,
                    [EntryType] nvarchar(max) NOT NULL,
                    [Description] nvarchar(max) NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [EntryDate] datetime2 NOT NULL,
                    CONSTRAINT [FK_MessFundEntries_MessGroups_MessGroupId] FOREIGN KEY ([MessGroupId]) REFERENCES [MessGroups]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_MessFundEntries_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([UserId])
                  );",
                @"IF OBJECT_ID('MessNotices', 'U') IS NULL
                  CREATE TABLE [MessNotices] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_MessNotices] PRIMARY KEY,
                    [MessGroupId] int NOT NULL,
                    [PostedByUserId] int NOT NULL,
                    [Title] nvarchar(120) NOT NULL,
                    [Body] nvarchar(1000) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [FK_MessNotices_MessGroups_MessGroupId] FOREIGN KEY ([MessGroupId]) REFERENCES [MessGroups]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_MessNotices_Users_PostedByUserId] FOREIGN KEY ([PostedByUserId]) REFERENCES [Users]([UserId])
                  );",
                @"IF OBJECT_ID('MessRosterItems', 'U') IS NULL
                  CREATE TABLE [MessRosterItems] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_MessRosterItems] PRIMARY KEY,
                    [MessGroupId] int NOT NULL,
                    [AssignedUserId] int NOT NULL,
                    [TaskType] nvarchar(max) NOT NULL,
                    [AssignedDate] datetime2 NOT NULL,
                    [MenuOrNotes] nvarchar(max) NULL,
                    [IsCompleted] bit NOT NULL,
                    [CompletedAt] datetime2 NULL,
                    CONSTRAINT [FK_MessRosterItems_MessGroups_MessGroupId] FOREIGN KEY ([MessGroupId]) REFERENCES [MessGroups]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_MessRosterItems_Users_AssignedUserId] FOREIGN KEY ([AssignedUserId]) REFERENCES [Users]([UserId])
                  );"
            };

            foreach (var sql in sqlStatements)
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
        }
    }
}
