using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BachelorRoomFinding.Entities
{
    public enum MessRole { Owner, MessAdmin, Tenant }

    public class MessGroup
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // The room/building this mess is associated with
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        public int? ManagerUserId { get; set; }
        public User? Manager { get; set; }

        [MaxLength(16)]
        public string InviteCode { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<MessMember> Members { get; set; } = new List<MessMember>();
        public ICollection<MessExpense> Expenses { get; set; } = new List<MessExpense>();
        public ICollection<MessNotice> Notices { get; set; } = new List<MessNotice>();
        public ICollection<MessRosterItem> Rosters { get; set; } = new List<MessRosterItem>();
        public ICollection<MessFundEntry> FundEntries { get; set; } = new List<MessFundEntry>();
        public ICollection<MessMenuVote> MenuVotes { get; set; } = new List<MessMenuVote>();
        public ICollection<MessDamageReport> DamageReports { get; set; } = new List<MessDamageReport>();
        public ICollection<MessMeterReading> MeterReadings { get; set; } = new List<MessMeterReading>();
        public ICollection<MessShoppingListItem> ShoppingListItems { get; set; } = new List<MessShoppingListItem>();
    }

    public class MessMember
    {
        public int Id { get; set; }
        
        public int MessGroupId { get; set; }
        public MessGroup? MessGroup { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public bool IsManager { get; set; }
        public MessRole Role { get; set; } = MessRole.Tenant;
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }

    public class MessExpense
    {
        public int Id { get; set; }

        public int MessGroupId { get; set; }
        public MessGroup? MessGroup { get; set; }

        public int AddedByUserId { get; set; }
        public User? AddedBy { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty; // e.g., "Bazar - Friday", "Gas Bill"

        [Required]
        public decimal Amount { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        // E.g. "Bazar", "Utility", "Internet"
        public string Category { get; set; } = "Bazar"; 
        public string? ReceiptImagePath { get; set; }

        public ICollection<MessExpenseShare> Shares { get; set; } = new List<MessExpenseShare>();
    }

    public class MessExpenseShare
    {
        public int Id { get; set; }
        
        public int MessExpenseId { get; set; }
        public MessExpense? MessExpense { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class MessNotice
    {
        public int Id { get; set; }
        public int MessGroupId { get; set; }
        public MessGroup? MessGroup { get; set; }
        public int PostedByUserId { get; set; }
        public User? PostedBy { get; set; }

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class MessRosterItem
    {
        public int Id { get; set; }
        public int MessGroupId { get; set; }
        public MessGroup? MessGroup { get; set; }
        public int AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        [Required]
        public string TaskType { get; set; } = "Cleaning"; // e.g. "Bazar", "Cleaning", "Cooking"
        public DateTime AssignedDate { get; set; } = DateTime.Today;
        public string? MenuOrNotes { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class MessFundEntry
    {
        public int Id { get; set; }
        public int MessGroupId { get; set; }
        public MessGroup? MessGroup { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public string EntryType { get; set; } = "Contribution"; // "Contribution" or "CommonExpense"
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime EntryDate { get; set; } = DateTime.Now;
    }

    public class MessMenuVote
    {
        public int Id { get; set; }
        public int MessGroupId { get; set; }
        public MessGroup? MessGroup { get; set; }
        public int CreatedByUserId { get; set; }
        public User? CreatedBy { get; set; }

        [Required, MaxLength(200)]
        public string OptionName { get; set; } = string.Empty; // e.g., "Chicken Biryani", "Fish Curry & Rice"
        public DateTime ProposedDate { get; set; } = DateTime.Today;
        public string MealType { get; set; } = "Lunch"; // Lunch or Dinner
        public int VoteCount { get; set; } = 0;
        public string VotedUserIdsCsv { get; set; } = string.Empty; // Comma-separated UserIds
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class MessDamageReport
    {
        public int Id { get; set; }
        public int MessGroupId { get; set; }
        public MessGroup? MessGroup { get; set; }
        public int ReportedByUserId { get; set; }
        public User? ReportedBy { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open"; // Open, InProgress, Fixed
        public DateTime ReportedAt { get; set; } = DateTime.Now;
    }

    public class MessMeterReading
    {
        public int Id { get; set; }
        public int MessGroupId { get; set; }
        public MessGroup? MessGroup { get; set; }
        public int LoggedByUserId { get; set; }
        public User? LoggedBy { get; set; }

        [Required]
        public string UtilityType { get; set; } = "Electricity"; // Electricity, Gas, Water
        public decimal CurrentReading { get; set; }
        public decimal PreviousReading { get; set; }
        public decimal BillAmount { get; set; }
        public DateTime ReadingDate { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
    }

    public class MessShoppingListItem
    {
        public int Id { get; set; }
        public int MessGroupId { get; set; }
        public MessGroup? MessGroup { get; set; }
        public int AddedByUserId { get; set; }
        public User? AddedBy { get; set; }

        [Required, MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;
        public string Quantity { get; set; } = "1 kg";
        public bool IsPurchased { get; set; }
        public int? PurchasedByUserId { get; set; }
        public User? PurchasedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
