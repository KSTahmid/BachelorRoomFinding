using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BachelorRoomFinding.Entities
{
    public class Review
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int ReviewerId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Safety Shield Additions
        public string? ReviewTags { get; set; } // Comma separated: Clean, Flexible, Strict, etc.
        public bool IsVerifiedTenantReview { get; set; } = false;

        [ValidateNever]
        public Room Room { get; set; } = null!;

        [ValidateNever]
        public User Reviewer { get; set; } = null!;
    }
}
