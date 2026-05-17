using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BachelorRoomFinding.Entities
{
    public class LoginHistory
    {
        public int Id { get; set; }
        public int? UserId { get; set; }   // nullable: failed logins have no matched user
        public DateTime LoginAt { get; set; } = DateTime.Now;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccess { get; set; }

        [ValidateNever]
        public User? User { get; set; }
    }
}
