# Bachelor Room Finder — ASP.NET Core MVC

## Architecture
- **Repository Pattern** — Generic `IRepository<T>` interface for clean data access
- **Server-Side Pagination** — `PagedResult<T>` with search, page size selector, and page number navigation
- **BCrypt password hashing** — passwords hashed on User creation

## Entities
| Entity | Key Fields |
|--------|-----------|
| Role   | Id, RoleName, RoleDescription |
| User   | UserId, UserName, Email, PasswordHash (BCrypt), Address, RoleId (FK), LastLogin |
| Room   | Id, Title, Description, Address, Rent, BedroomCount, IsAvailable, PostedDate, OwnerId (FK) |

## Setup

### 1. Update connection string in `appsettings.json`
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BachelorRoomFinderDb;Trusted_Connection=True"
```

### 2. Run EF Core migrations
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Run the project
```bash
dotnet run
```

## CRUD Endpoints

| Controller | Route            | Description              |
|------------|-----------------|--------------------------|
| Role       | /Role           | List with pagination     |
| Role       | /Role/Create    | Create new role          |
| Role       | /Role/Edit/{id} | Edit existing role       |
| Role       | /Role/Delete/{id}| Confirm & delete role   |
| User       | /User           | List with pagination     |
| User       | /User/Create    | Create new user          |
| User       | /User/Edit/{id} | Edit existing user       |
| User       | /User/Delete/{id}| Confirm & delete user   |
| Room       | /Room           | List with pagination     |
| Room       | /Room/Create    | Post a room              |
| Room       | /Room/Edit/{id} | Edit room listing        |
| Room       | /Room/Delete/{id}| Remove room listing     |

## Namespace Structure
```
BachelorRoomFinding/
├── Entities/          → BachelorRoomFinding.Entities
│   ├── Role.cs
│   ├── User.cs
│   └── Room.cs
├── Models/            → BachelorRoomFinding.Models
│   ├── PagedResult<T>.cs
│   └── ErrorViewModel.cs
├── Interfaces/        → BachelorRoomFinding.Interfaces
│   └── IRepository<T>.cs
├── Repositories/      → BachelorRoomFinding.Repositories
│   ├── RoleRepository.cs
│   ├── UserRepository.cs
│   └── RoomRepository.cs
├── Controllers/       → BachelorRoomFinding.Controllers
│   ├── RoleController.cs
│   ├── UserController.cs
│   ├── RoomController.cs
│   └── AccountController.cs
├── AppDbContext.cs     → BachelorRoomFinding.Data
└── Views/
    ├── Role/          → Index, Create, Edit, Delete
    ├── User/          → Index, Create, Edit, Delete
    └── Room/          → Index, Create, Edit, Delete
```
