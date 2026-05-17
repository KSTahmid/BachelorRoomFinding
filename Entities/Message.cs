using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BachelorRoomFinding.Entities
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public int? RoomId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;

        [ValidateNever]
        public User Sender { get; set; } = null!;

        [ValidateNever]
        public User Receiver { get; set; } = null!;

        [ValidateNever]
        public Room? Room { get; set; }
    }
}
