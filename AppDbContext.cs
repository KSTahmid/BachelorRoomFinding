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

        // Mess Committee Entities
        public DbSet<MessGroup>            MessGroups            { get; set; }
        public DbSet<MessMember>           MessMembers           { get; set; }
        public DbSet<MessExpense>          MessExpenses          { get; set; }
        public DbSet<MessExpenseShare>     MessExpenseShares     { get; set; }
        public DbSet<MessNotice>           MessNotices           { get; set; }
        public DbSet<MessRosterItem>       MessRosterItems       { get; set; }
        public DbSet<MessFundEntry>        MessFundEntries       { get; set; }
        public DbSet<MessMenuVote>         MessMenuVotes         { get; set; }
        public DbSet<MessDamageReport>     MessDamageReports     { get; set; }
        public DbSet<MessMeterReading>     MessMeterReadings     { get; set; }
        public DbSet<MessShoppingListItem> MessShoppingListItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── User → Role ─────────────────────────────────────────
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .Property(u => u.IsDemoNumber)
                .HasDefaultValue(true);

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
                .Property(p => p.ApplicationId)
                .HasColumnName("PaymentId");
                
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

            // Prevent duplicate bKash/Nagad transaction codes across all payments
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TransactionId)
                .IsUnique()
                .HasFilter("[TransactionId] IS NOT NULL");  // NULL is allowed (bank transfer may omit)

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Room)
                .WithMany()
                .HasForeignKey(p => p.RoomId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

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
                .Property(a => a.MaxRentPerPerson)
                .HasColumnType("decimal(18,2)");

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

            // ── MessGroup ───────────────────────────────────────────
            modelBuilder.Entity<MessGroup>()
                .HasOne(m => m.Room)
                .WithMany()
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessGroup>()
                .HasOne(m => m.Manager)
                .WithMany()
                .HasForeignKey(m => m.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessGroup>()
                .HasIndex(m => m.RoomId)
                .IsUnique();

            modelBuilder.Entity<MessGroup>()
                .HasIndex(m => m.InviteCode)
                .IsUnique();

            modelBuilder.Entity<MessGroup>()
                .Property(m => m.InviteCode)
                .HasMaxLength(16);

            // ── MessMember ──────────────────────────────────────────
            modelBuilder.Entity<MessMember>()
                .HasOne(m => m.MessGroup)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.MessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessMember>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessMember>()
                .HasIndex(m => new { m.MessGroupId, m.UserId })
                .IsUnique();

            // ── MessExpense ─────────────────────────────────────────
            modelBuilder.Entity<MessExpense>()
                .Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MessExpense>()
                .HasOne(e => e.MessGroup)
                .WithMany(g => g.Expenses)
                .HasForeignKey(e => e.MessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessExpense>()
                .HasOne(e => e.AddedBy)
                .WithMany()
                .HasForeignKey(e => e.AddedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── MessExpenseShare ────────────────────────────────────
            modelBuilder.Entity<MessExpenseShare>()
                .Property(s => s.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MessExpenseShare>()
                .HasOne(s => s.MessExpense)
                .WithMany(e => e.Shares)
                .HasForeignKey(s => s.MessExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessExpenseShare>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessNotice>()
                .HasOne(n => n.MessGroup)
                .WithMany(g => g.Notices)
                .HasForeignKey(n => n.MessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessNotice>()
                .HasOne(n => n.PostedBy)
                .WithMany()
                .HasForeignKey(n => n.PostedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessRosterItem>()
                .HasOne(r => r.MessGroup)
                .WithMany(g => g.Rosters)
                .HasForeignKey(r => r.MessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessRosterItem>()
                .HasOne(r => r.AssignedUser)
                .WithMany()
                .HasForeignKey(r => r.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessFundEntry>()
                .Property(f => f.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MessFundEntry>()
                .HasOne(f => f.MessGroup)
                .WithMany(g => g.FundEntries)
                .HasForeignKey(f => f.MessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessFundEntry>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── MessMenuVote ─────────────────────────────────────────
            modelBuilder.Entity<MessMenuVote>()
                .HasOne(v => v.MessGroup)
                .WithMany(g => g.MenuVotes)
                .HasForeignKey(v => v.MessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessMenuVote>()
                .HasOne(v => v.CreatedBy)
                .WithMany()
                .HasForeignKey(v => v.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── MessDamageReport ─────────────────────────────────────
            modelBuilder.Entity<MessDamageReport>()
                .HasOne(d => d.MessGroup)
                .WithMany(g => g.DamageReports)
                .HasForeignKey(d => d.MessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessDamageReport>()
                .HasOne(d => d.ReportedBy)
                .WithMany()
                .HasForeignKey(d => d.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── MessMeterReading ─────────────────────────────────────
            modelBuilder.Entity<MessMeterReading>()
                .Property(m => m.CurrentReading).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MessMeterReading>()
                .Property(m => m.PreviousReading).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MessMeterReading>()
                .Property(m => m.BillAmount).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MessMeterReading>()
                .HasOne(m => m.MessGroup)
                .WithMany(g => g.MeterReadings)
                .HasForeignKey(m => m.MessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessMeterReading>()
                .HasOne(m => m.LoggedBy)
                .WithMany()
                .HasForeignKey(m => m.LoggedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── MessShoppingListItem ─────────────────────────────────
            modelBuilder.Entity<MessShoppingListItem>()
                .HasOne(s => s.MessGroup)
                .WithMany(g => g.ShoppingListItems)
                .HasForeignKey(s => s.MessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessShoppingListItem>()
                .HasOne(s => s.AddedBy)
                .WithMany()
                .HasForeignKey(s => s.AddedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessShoppingListItem>()
                .HasOne(s => s.PurchasedBy)
                .WithMany()
                .HasForeignKey(s => s.PurchasedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
