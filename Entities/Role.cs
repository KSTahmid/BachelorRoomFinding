namespace BachelorRoomFinding.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
