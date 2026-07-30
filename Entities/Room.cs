using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BachelorRoomFinding.Entities
{
    public enum RoomType { MaleMess, FemaleHostel, SharedSeat, SingleRoom, Sublet, BachelorFlat }
    public enum RoomStatus { Draft, PendingApproval, Active, Rented, Inactive }

    public class Room
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Thana { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SeatRent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ElectricityBill { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WiFiBill { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GasBill { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WaterBill { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceCharge { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MealCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Advance { get; set; }

        public int BedroomCount { get; set; }
        public RoomType RoomType { get; set; } = RoomType.MaleMess;
        public RoomStatus Status { get; set; } = RoomStatus.Draft;
        public bool IsAvailable { get; set; } = true;
        public DateTime PostedDate { get; set; } = DateTime.Now;
        public DateTime? AvailableFrom { get; set; }
        public int ViewCount { get; set; } = 0;

        // Rules: pipe-separated e.g. "No Smoking|No Pets|Female Only"
        public string? Rules { get; set; }

        // Optional map coordinates
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Safety Shield
        public int SafetyScore { get; set; } = 80; // Default base score

        public int OwnerId { get; set; }

        [ValidateNever]
        public User Owner { get; set; } = null!;

        [ValidateNever]
        public ICollection<RoomPhoto> Photos { get; set; } = new List<RoomPhoto>();

        [ValidateNever]
        public ICollection<RoomFacility> Facilities { get; set; } = new List<RoomFacility>();

        [ValidateNever]
        public ICollection<RentalApplication> Applications { get; set; } = new List<RentalApplication>();

        [ValidateNever]
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        [ValidateNever]
        public ICollection<RoomView> Views { get; set; } = new List<RoomView>();

        [ValidateNever]
        public ICollection<SavedRoom> SavedByUsers { get; set; } = new List<SavedRoom>();
    }
}
