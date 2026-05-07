namespace BachelorRoomFinding.Entities
{
    public class Room
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Rent { get; set; }
        public int BedroomCount { get; set; }
        public bool IsAvailable { get; set; } = true;
        public DateTime PostedDate { get; set; } = DateTime.Now;

        public int OwnerId { get; set; }
        public User Owner { get; set; } = null!;
    }
}
