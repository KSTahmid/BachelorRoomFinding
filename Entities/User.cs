using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BachelorRoomFinding.Entities
{
    public enum AccountStatus { Active, Suspended, Pending }

    public class User
    {
        public int UserId { get; set; }

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePhotoPath { get; set; }
        public string? BkashNumber { get; set; }
        public string? NagadNumber { get; set; }
        public bool IsDemoNumber { get; set; } = true;

        public bool IsApprovedByAdmin { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public bool IsEmailVerified { get; set; } = false;
        public AccountStatus AccountStatus { get; set; } = AccountStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }
        public string? EmailVerificationToken { get; set; }

        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        [ValidateNever]
        public virtual Role Role { get; set; } = null!;

        [ValidateNever]
        public ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();

        [ValidateNever]
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
