using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Repositories;
using BachelorRoomFinding.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Core Repositories ─────────────────────────────────────────
builder.Services.AddScoped<IRepository<Role>, RoleRepository>();
builder.Services.AddScoped<IRepository<User>, UserRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRepository<Room>>(sp => sp.GetRequiredService<IRoomRepository>());

// ── Feature Repositories ──────────────────────────────────────
builder.Services.AddScoped<IRepository<KycDocument>, KycRepository>();
builder.Services.AddScoped<IRepository<RentalApplication>, ApplicationRepository>();
builder.Services.AddScoped<IRepository<Payment>, PaymentRepository>();
builder.Services.AddScoped<IRepository<LoginHistory>, LoginHistoryRepository>();
builder.Services.AddScoped<IRepository<SavedRoom>, SavedRoomRepository>();
builder.Services.AddScoped<IRepository<Review>, ReviewRepository>();
builder.Services.AddScoped<IRepository<Notification>, NotificationRepository>();
builder.Services.AddScoped<IRepository<RoommateAd>, RoommateAdRepository>();
builder.Services.AddScoped<MessageRepository>();

// ── Services ──────────────────────────────────────────────────
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<FakeEmailService>();

var app = builder.Build();

// ── Database Seeding ──────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Seeding failed: {Message}", ex.Message);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();