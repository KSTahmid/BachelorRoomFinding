using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BachelorRoomFinding.Entities
{
    public class RoomView
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int? ViewerUserId { get; set; }
        public string? SessionId { get; set; }
        public DateTime ViewedAt { get; set; } = DateTime.Now;

        [ValidateNever]
        public Room Room { get; set; } = null!;

        [ValidateNever]
        public User? ViewerUser { get; set; }
    }
}
