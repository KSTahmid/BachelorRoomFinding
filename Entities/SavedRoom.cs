using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BachelorRoomFinding.Entities
{
    public class SavedRoom
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoomId { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.Now;

        [ValidateNever]
        public User User { get; set; } = null!;

        [ValidateNever]
        public Room Room { get; set; } = null!;
    }
}
