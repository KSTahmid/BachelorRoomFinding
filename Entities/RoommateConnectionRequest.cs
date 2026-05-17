using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BachelorRoomFinding.Entities
{
    public enum ConnectionRequestStatus { Pending, Accepted, Rejected }

    public class RoommateConnectionRequest
    {
        public int Id { get; set; }
        
        public int SenderUserId { get; set; }
        public int RoommateAdId { get; set; }
        
        [Required, MaxLength(500)]
        public string Message { get; set; } = string.Empty;
        
        public ConnectionRequestStatus Status { get; set; } = ConnectionRequestStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ValidateNever]
        [ForeignKey("SenderUserId")]
        public virtual User Sender { get; set; } = null!;

        [ValidateNever]
        [ForeignKey("RoommateAdId")]
        public virtual RoommateAd RoommateAd { get; set; } = null!;
    }
}
