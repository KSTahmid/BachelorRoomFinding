using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BachelorRoomFinding.Entities
{
    public enum RoommateAdStatus { Active, Closed }

    public class RoommateAd
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [Required]
        public string PreferredAreas { get; set; } = string.Empty; // Comma separated or simple string
        
        [Required]
        public decimal MaxRentPerPerson { get; set; }
        
        [Required]
        public DateTime MoveInDate { get; set; }
        
        public int NumberOfRoommatesNeeded { get; set; } = 1;
        
        public int? RoomId { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvancePaymentAmount { get; set; }
        
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public RoommateAdStatus Status { get; set; } = RoommateAdStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ValidateNever]
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ValidateNever]
        public ICollection<RoommateConnectionRequest> ConnectionRequests { get; set; } = new List<RoommateConnectionRequest>();

        [ValidateNever]
        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }
    }
}
