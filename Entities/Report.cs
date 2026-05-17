using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BachelorRoomFinding.Entities
{
    public enum ReportReason { SuspiciousListing, FakePhotos, MisleadingInfo, Harassment, Other }
    public enum ReportStatus { New, UnderReview, Resolved, Dismissed }

    public class Report
    {
        public int Id { get; set; }
        
        public int ReporterUserId { get; set; }
        
        // Either RoomId or TargetUserId (or both)
        public int? TargetRoomId { get; set; }
        public int? TargetUserId { get; set; }
        
        [Required]
        public ReportReason Reason { get; set; }
        
        [Required, MaxLength(1000)]
        public string Details { get; set; } = string.Empty;
        
        public ReportStatus Status { get; set; } = ReportStatus.New;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? AdminNote { get; set; }

        [ValidateNever]
        [ForeignKey("ReporterUserId")]
        public virtual User Reporter { get; set; } = null!;

        [ValidateNever]
        [ForeignKey("TargetRoomId")]
        public virtual Room? TargetRoom { get; set; }

        [ValidateNever]
        [ForeignKey("TargetUserId")]
        public virtual User? TargetUser { get; set; }
    }
}
