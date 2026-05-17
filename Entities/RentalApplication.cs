using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BachelorRoomFinding.Entities
{
    public enum ApplicationStatus { Pending, Approved, Rejected, Cancelled }

    public class RentalApplication
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int ApplicantId { get; set; }

        public DateTime MoveInDate { get; set; }
        public int DurationMonths { get; set; } = 1;
        public string? Message { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public DateTime AppliedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }

        [ValidateNever]
        public Room Room { get; set; } = null!;

        [ValidateNever]
        public User Applicant { get; set; } = null!;

        [ValidateNever]
        public Payment? Payment { get; set; }
    }
}
