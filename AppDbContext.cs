using BachelorRoomFinding.Entities;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role>              Roles              { get; set; }
        public DbSet<User>              Users              { get; set; }
        public DbSet<Room>              Rooms              { get; set; }
        public DbSet<KycDocument>       KycDocuments       { get; set; }
        public DbSet<RentalApplication> RentalApplications { get; set; }
        public DbSet<Payment>           Payments           { get; set; }
        public DbSet<LoginHistory>      LoginHistories     { get; set; }
        public DbSet<RoomView>          RoomViews          { get; set; }
        public DbSet<SavedRoom>         SavedRooms         { get; set; }
        public DbSet<Review>            Reviews            { get; set; }
        public DbSet<Message>           Messages           { get; set; }
        public DbSet<Notification>      Notifications      { get; set; }
        public DbSet<RoomPhoto>         RoomPhotos         { get; set; }
        public DbSet<RoomFacility>      RoomFacilities     { get; set; }

        // New Feature Entities
        public DbSet<UserPreference>    UserPreferences    { get; set; }
        public DbSet<RoommateAd>        RoommateAds        { get; set; }
        public DbSet<RoommateConnectionRequest> RoommateConnectionRequests { get; set; }
        public DbSet<Report>            Reports            { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── User → Role ─────────────────────────────────────────
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Room → Owner ─────────────────────────────────────────
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Owner)
                .WithMany()
                .HasForeignKey(r => r.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── KycDocument → User (owner of doc) ───────────────────
            // Using Restrict on BOTH to avoid multi-cascade-path from Users → KycDocuments
            modelBuilder.Entity<KycDocument>()
                .HasOne(k => k.User)
                .WithMany()
                .HasForeignKey(k => k.UserId)
                .OnDelete(DeleteBehavior.Restrict);          // ← was Cascade; changed to avoid cycle

            modelBuilder.Entity<KycDocument>()
                .HasOne(k => k.ReviewedBy)
                .WithMany()
                .HasForeignKey(k => k.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);          // ← was SetNull; changed to avoid cycle

            // ── RentalApplication → Room ─────────────────────────────
            modelBuilder.Entity<RentalApplication>()
                .HasOne(a => a.Room)
                .WithMany(r => r.Applications)
                .HasForeignKey(a => a.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── RentalApplication → Applicant ─────────────────────────
            modelBuilder.Entity<RentalApplication>()
                .HasOne(a => a.Applicant)
                .WithMany()
                .HasForeignKey(a => a.ApplicantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Payment → Application (1-to-1) ───────────────────────
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Application)
                .WithOne(a => a.Payment)
                .HasForeignKey<Payment>(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ConfirmedBy)
                .WithMany()
                .HasForeignKey(p => p.ConfirmedByUserId)
                .OnDelete(DeleteBehavior.Restrict);         // ← was SetNull; Restrict is safer

            // ── LoginHistory → User (nullable FK) ────────────────────
            modelBuilder.Entity<LoginHistory>()
                .HasOne(l => l.User)
                .WithMany(u => u.LoginHistories)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);         // nullable FK, Restrict is fine

            // ── RoomView → Room ───────────────────────────────────────
            modelBuilder.Entity<RoomView>()
                .HasOne(v => v.Room)
                .WithMany(r => r.Views)
                .HasForeignKey(v => v.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoomView>()
                .HasOne(v => v.ViewerUser)
                .WithMany()
                .HasForeignKey(v => v.ViewerUserId)
                .OnDelete(DeleteBehavior.Restrict);         // ← was SetNull

            // ── SavedRoom ──────────────────────────────────────────────
            modelBuilder.Entity<SavedRoom>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SavedRoom>()
                .HasOne(s => s.Room)
                .WithMany(r => r.SavedByUsers)
                .HasForeignKey(s => s.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Review ─────────────────────────────────────────────────
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Room)
                .WithMany(r => r.Reviews)
                .HasForeignKey(r => r.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Message ────────────────────────────────────────────────
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Room)
                .WithMany()
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Restrict);         // ← was SetNull

            // ── Notification → User ────────────────────────────────────
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── RoomPhoto → Room ───────────────────────────────────────
            modelBuilder.Entity<RoomPhoto>()
                .HasOne(p => p.Room)
                .WithMany(r => r.Photos)
                .HasForeignKey(p => p.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── RoomFacility → Room ────────────────────────────────────
            modelBuilder.Entity<RoomFacility>()
                .HasOne(f => f.Room)
                .WithMany(r => r.Facilities)
                .HasForeignKey(f => f.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── UserPreference ───────────────────────────────────────
            modelBuilder.Entity<UserPreference>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<UserPreference>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── RoommateAd ───────────────────────────────────────────
            modelBuilder.Entity<RoommateAd>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── RoommateConnectionRequest ────────────────────────────
            modelBuilder.Entity<RoommateConnectionRequest>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoommateConnectionRequest>()
                .HasOne(c => c.RoommateAd)
                .WithMany(a => a.ConnectionRequests)
                .HasForeignKey(c => c.RoommateAdId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Report ───────────────────────────────────────────────
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.TargetRoom)
                .WithMany()
                .HasForeignKey(r => r.TargetRoomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.TargetUser)
                .WithMany()
                .HasForeignKey(r => r.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
