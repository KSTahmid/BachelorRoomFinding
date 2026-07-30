# 🏠 Bachelor Room Finding — ASP.NET Core MVC

> **Bangladesh's Smart Room Rental Platform** — Find, Rent, and Manage Bachelor Rooms Digitally.

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-blue?logo=dotnet)
![Entity Framework](https://img.shields.io/badge/EF%20Core-8.0-purple)
![SQL Server](https://img.shields.io/badge/SQL%20Server-LocalDB-red?logo=microsoftsqlserver)
![License](https://img.shields.io/badge/License-MIT-green)

---

## 📋 Project Overview

**Bachelor Room Finding** is a full-stack web application built with ASP.NET Core MVC that connects **room owners** and **tenants** in Bangladesh. The platform provides end-to-end rental management — from room listing and searching to online payment processing and mess board collaboration.

### 🎯 Key Features

| Feature | Description |
|---------|-------------|
| 🔐 **Authentication** | Secure login, registration, email verification, and forgot password |
| 🏘️ **Room Listings** | Owners can post rooms with photos, facilities, rent details |
| 🔍 **Smart Search** | Filter by district, thana, room type, price range |
| 📋 **Rental Applications** | Tenants apply for rooms; owners approve/reject |
| 💳 **Online Payment** | bKash and Nagad payment gateway integration |
| 🤝 **Roommate Finder** | Connect with potential roommates via chat |
| 🏠 **Mess Board** | Manage mess groups, expenses, shopping lists, duties |
| 🔔 **Notifications** | Real-time notifications for all key events |
| 🛡️ **KYC Verification** | Document upload and admin verification |
| 📊 **Admin Dashboard** | Full platform oversight and user management |

---

## 🏗️ Architecture

```
BachelorRoomFinding/
├── Controllers/           → MVC Controllers (Account, Room, Owner, Admin, Payment, Mess...)
├── Entities/              → Domain models (User, Room, Payment, MessGroup...)
├── Interfaces/            → Service and Repository contracts
├── Repositories/          → EF Core data access layer
├── Services/              → Business logic (Payment, OTP, Email...)
├── ViewModels/            → View-specific data transfer objects
├── Views/                 → Razor .cshtml templates
├── Data/
│   ├── AppDbContext.cs    → EF Core DbContext with all entity configurations
│   └── SeedData.cs        → Database seeder with default admin, owner, tenant accounts
├── Migrations/            → EF Core migration history
├── Filters/               → Action filters (e.g. LoginRequired)
├── wwwroot/               → Static assets (CSS, JS, images)
├── appsettings.json       → Application configuration
└── Program.cs             → App startup, DI registration, middleware pipeline
```

### Design Patterns Used
- **Repository Pattern** — `IRepository<T>` for clean, testable data access
- **Service Layer** — Business logic separated from controllers
- **Generic Pagination** — `PagedResult<T>` with search, sort, and page navigation
- **BCrypt Hashing** — Secure password hashing on user creation
- **Session-based Auth** — Lightweight session management for user state

---

## ⚙️ Setup & Installation

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/) or SQL Server LocalDB
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code

### Step 1 — Clone the Repository
```bash
git clone https://github.com/KSTahmid/BachelorRoomFinding.git
cd BachelorRoomFinding
```

### Step 2 — Configure Database Connection
Edit `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BachelorRoomFinderDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

> For SQL Server Express, replace `(localdb)\\mssqllocaldb` with `YOUR_SERVER_NAME\\SQLEXPRESS`

### Step 3 — Apply Migrations
```bash
dotnet ef database update
```
> The database will be created and seeded automatically with default accounts.

### Step 4 — Run the Project
```bash
dotnet run
```
Then open: **http://localhost:5108**

---

## 👤 Default Login Credentials

> All accounts use password: **`123456`**

| Role | Email | Description |
|------|-------|-------------|
| **Admin** | admin1@brf.com | Super Administrator |
| **Admin** | admin2@brf.com | Support Administrator |
| **Owner** | raktim305@gmail.com | Primary Owner Account |
| **Owner** | owner2@brf.com | Owner Account |
| **Tenant** | raktimwhattsapp@gmail.com | Primary Tenant Account |
| **Tenant** | user2@brf.com | Tenant Account |

---

## 💳 Payment Flow

1. Tenant applies for a room → Owner approves the application
2. Tenant goes to **Pay Advance** page
3. Select **bKash** or **Nagad**
4. Enter account number → Verification code → PIN
5. Payment recorded → Room marked as **Rented** ✅

---

## 🗄️ Key Database Entities

| Entity | Key Fields |
|--------|-----------|
| `User` | UserId, UserName, Email, PasswordHash, RoleId, BkashNumber, NagadNumber |
| `Room` | Id, Title, Address, MonthlyRent, Advance, RoomType, OwnerId |
| `RentalApplication` | Id, RoomId, ApplicantId, Status, MoveInDate |
| `Payment` | Id, ApplicationId, Method, Amount, TransactionId, Status |
| `MessGroup` | Id, RoomId, InviteCode, Members |
| `KycDocument` | Id, UserId, NidFrontPath, NidBackPath, Status |
| `Notification` | Id, UserId, Title, Type, IsRead |

---

## 🧰 Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend Framework | ASP.NET Core 8 MVC |
| ORM | Entity Framework Core 8 |
| Database | SQL Server / LocalDB |
| Authentication | Session-based + BCrypt |
| Frontend | Bootstrap 5, Vanilla JS |
| Email | SMTP (configured in appsettings) |
| Password Hashing | BCrypt.Net |

---

## 👥 Contributors

| GitHub | Role |
|--------|------|
| [@KSTahmid](https://github.com/KSTahmid) | Core Backend, Architecture, EF Core, Authentication |
| [@raktim3050](https://github.com/raktim3050) | Payment Module, Mess Board, UI/UX, Frontend Integration |

---

## 📄 License

This project is licensed under the **MIT License**.

---

*Built with ❤️ for the students of Bangladesh 🇧🇩*
