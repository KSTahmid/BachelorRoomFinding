using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Repositories;
using BachelorRoomFinding.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// ==========================================
// Bachelor Room Finding Startup Configuration
// ==========================================
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
try
{
    var csb = new SqlConnectionStringBuilder(defaultConnection);
    if (csb.ConnectTimeout > 15) csb.ConnectTimeout = 15;
    defaultConnection = csb.ConnectionString;
}
catch
{
    // Keep the configured connection string if it is not a SqlConnectionStringBuilder-compatible value.
}

var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("MessBashaTest");
    }
    else
    {
        options.UseSqlServer(defaultConnection, sql => sql.CommandTimeout(20));
    }
});

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
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// ── Services ──────────────────────────────────────────────────
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

var app = builder.Build();

// ── Database Migration & Seeding ──────────────────────────────
// Run this after Kestrel starts so a slow/unavailable SQL Server does not make
// the whole site look unchanged or dead during presentation.
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            await SeedData.Initialize(services);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Database migration/seeding failed: {Message}", ex.Message);
        }
    });
});

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
