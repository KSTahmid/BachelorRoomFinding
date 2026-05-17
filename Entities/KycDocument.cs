using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BachelorRoomFinding.Entities
{
    public enum KycStatus { Pending, Approved, Rejected }

    public class KycDocument
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        public string NationalIdNumber { get; set; } = string.Empty;

        public string? NidFrontPath { get; set; }
        public string? NidBackPath { get; set; }
        public string? FacePhotoPath { get; set; }

        public KycStatus Status { get; set; } = KycStatus.Pending;
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByUserId { get; set; }
        public string? ReviewNote { get; set; }

        [ValidateNever]
        public User User { get; set; } = null!;

        [ValidateNever]
        public User? ReviewedBy { get; set; }
    }
}
