using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Models;

namespace BachelorRoomFinding.Interfaces
{
    public interface IRoomRepository : IRepository<Room>
    {
        Task<IEnumerable<Room>> GetOwnerRoomsAsync(int ownerId);
        Task<IEnumerable<Room>> GetActiveRoomsAsync();
        Task<PagedResult<Room>> GetFilteredAsync(
            int pageNumber, int pageSize,
            string? search = null,
            string? district = null,
            string? thana = null,
            RoomType? roomType = null,
            decimal? minRent = null,
            decimal? maxRent = null,
            bool? availableNow = null,
            string? sortBy = null,
            List<string>? facilities = null);
    }
}
