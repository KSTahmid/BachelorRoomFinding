using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BachelorRoomFinding.Entities
{
    public enum PaymentMethod { bKash, Nagad, BankTransfer }
    public enum PaymentStatus { Pending, Confirmed }

    public class Payment
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }

        public PaymentMethod Method { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? TransactionId { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime PaidAt { get; set; } = DateTime.Now;
        public int? ConfirmedByUserId { get; set; }

        [ValidateNever]
        public RentalApplication Application { get; set; } = null!;

        [ValidateNever]
        public User? ConfirmedBy { get; set; }
    }
}
