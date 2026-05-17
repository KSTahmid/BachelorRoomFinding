using BachelorRoomFinding.Entities;

namespace BachelorRoomFinding.ViewModels
{
    public class RoomFilterViewModel
    {
        public string? Search { get; set; }
        public string? District { get; set; }
        public string? Thana { get; set; }
        public RoomType? RoomType { get; set; }
        public decimal? MinRent { get; set; }
        public decimal? MaxRent { get; set; }
        public bool? AvailableNow { get; set; }
        public string? SortBy { get; set; }
        public List<string> Facilities { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
    }
}
