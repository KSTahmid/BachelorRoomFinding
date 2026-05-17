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

        [Required, Range(500, 1000000)]
        public decimal Rent { get; set; }

        [Range(0, 1000000)]
        public decimal SecurityDeposit { get; set; }

        [Range(0, 1000000)]
        public decimal Advance { get; set; }

        [Range(1, 20)]
        public int BedroomCount { get; set; } = 1;

        public RoomType RoomType { get; set; } = RoomType.Single;
        public DateTime? AvailableFrom { get; set; }

        // Facilities - checklist
        public List<string> SelectedFacilities { get; set; } = new();

        // Rules
        public bool NoSmoking { get; set; }
        public bool NoPets { get; set; }
        public string GenderRule { get; set; } = "Any"; // Male / Female / Any

        // Photos
        public List<IFormFile>? PhotoFiles { get; set; }

        // For edit
        public int OwnerId { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.Draft;
    }
}
