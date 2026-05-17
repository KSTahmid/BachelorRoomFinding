using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BachelorRoomFinding.Entities
{
    public enum NotificationType { ApplicationStatus, KycApproval, NewMessage, PaymentConfirmed, General }

    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string NotificationMessage { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.General;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ValidateNever]
        public User User { get; set; } = null!;
    }
}
