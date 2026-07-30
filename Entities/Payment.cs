using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BachelorRoomFinding.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public int? UserId { get; set; }

        public string Method { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? TransactionId { get; set; }
        public int? OwnerId { get; set; }
        public int? RoomId { get; set; }
        public string? SenderWalletNumber { get; set; }
        public string? RecipientWalletNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }

        public string Status { get; set; } = "Pending";
        
        public string? OtpCode { get; set; }
        public bool IsOtpVerified { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? VerifiedAt { get; set; }
        
        public int? ConfirmedByUserId { get; set; }

        [ValidateNever]
        public RentalApplication Application { get; set; } = null!;

        [ValidateNever]
        public User? ConfirmedBy { get; set; }
        
        [ValidateNever]
        public User? User { get; set; }

        [ValidateNever]
        public User? Owner { get; set; }

        [ValidateNever]
        public Room? Room { get; set; }
    }
}
