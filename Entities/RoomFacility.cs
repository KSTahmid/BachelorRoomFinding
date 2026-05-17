using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BachelorRoomFinding.Entities
{
    public class RoomFacility
    {
        public int Id { get; set; }
        public int RoomId { get; set; }

        [Required]
        public string FacilityName { get; set; } = string.Empty;

        [ValidateNever]
        public Room Room { get; set; } = null!;
    }
}
