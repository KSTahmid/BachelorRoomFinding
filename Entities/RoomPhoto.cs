using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BachelorRoomFinding.Entities
{
    public class RoomPhoto
    {
        public int Id { get; set; }
        public int RoomId { get; set; }

        [Required]
        public string PhotoPath { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;
        public bool IsVideo { get; set; } = false;
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [ValidateNever]
        public Room Room { get; set; } = null!;
    }
}
