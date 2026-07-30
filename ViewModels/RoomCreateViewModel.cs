using BachelorRoomFinding.Entities;
using System.ComponentModel.DataAnnotations;

namespace BachelorRoomFinding.ViewModels
{
    public class RoomCreateViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string District { get; set; } = string.Empty;

        [Required]
        public string Thana { get; set; } = string.Empty;

        [Required, Range(0, 1000000)]
        public decimal MonthlyRent { get; set; }

        [Range(0, 100000)]
        public decimal SeatRent { get; set; }

        [Range(0, 100000)]
        public decimal ElectricityBill { get; set; }

        [Range(0, 100000)]
        public decimal WiFiBill { get; set; }

        [Range(0, 100000)]
        public decimal GasBill { get; set; }

        [Range(0, 100000)]
        public decimal WaterBill { get; set; }

        [Range(0, 100000)]
        public decimal ServiceCharge { get; set; }

        [Range(0, 100000)]
        public decimal MealCost { get; set; }

        [Range(0, 1000000)]
        public decimal SecurityDeposit { get; set; }

        [Range(0, 1000000)]
        public decimal Advance { get; set; }

        [Range(1, 20)]
        public int BedroomCount { get; set; } = 1;

        public RoomType RoomType { get; set; } = RoomType.MaleMess;
        public DateTime? AvailableFrom { get; set; }

        // Facilities - checklist
        public List<string> SelectedFacilities { get; set; } = new();

        // Rules
        public bool SmokingAllowed { get; set; }
        public bool GuestAllowed { get; set; }
        public string CurfewTiming { get; set; } = string.Empty;
        public bool BachelorOnly { get; set; }
        public bool FamilyRestricted { get; set; }

        // Media (Photos/Videos)
        public List<IFormFile>? MediaFiles { get; set; }

        // For edit
        public int OwnerId { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.Draft;
    }
}
