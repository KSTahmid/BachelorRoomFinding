namespace BachelorRoomFinding.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int RoleId { get; set; }
        public DateTime? LastLogin { get; set; }

        public Role Role { get; set; } = null!;
    }
}
