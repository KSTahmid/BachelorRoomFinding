using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly AppDbContext _context;
        public RoomRepository(AppDbContext context) => _context = context;

        private IQueryable<Room> BaseQuery() =>
            _context.Rooms
                .Include(r => r.Owner)
                .Include(r => r.Photos)
                .Include(r => r.Facilities);

        public async Task<PagedResult<Room>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, string? search = null)
        {
            var query = BaseQuery();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.Title.Contains(search) ||
                                         r.Address.Contains(search) ||
                                         r.District.Contains(search) ||
                                         r.Description.Contains(search));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(r => r.PostedDate)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<Room> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
        }

        public async Task<Room?> GetByIdAsync(int id) =>
            await BaseQuery().Include(r => r.Reviews).ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<IEnumerable<Room>> GetAllAsync() =>
            await BaseQuery().OrderByDescending(r => r.PostedDate).ToListAsync();

        public async Task AddAsync(Room entity)
        {
            await _context.Rooms.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Room entity)
        {
            _context.Rooms.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null) { _context.Rooms.Remove(room); await _context.SaveChangesAsync(); }
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Rooms.AnyAsync(r => r.Id == id);

        public async Task<IEnumerable<Room>> GetOwnerRoomsAsync(int ownerId) =>
            await BaseQuery().Where(r => r.OwnerId == ownerId)
                .OrderByDescending(r => r.PostedDate).ToListAsync();

        public async Task<IEnumerable<Room>> GetActiveRoomsAsync() =>
            await BaseQuery().Where(r => r.Status == RoomStatus.Active)
                .OrderByDescending(r => r.PostedDate).ToListAsync();

        public async Task<PagedResult<Room>> GetFilteredAsync(
            int pageNumber, int pageSize,
            string? search = null, string? district = null, string? thana = null,
            RoomType? roomType = null, decimal? minRent = null, decimal? maxRent = null,
            bool? availableNow = null, string? sortBy = null, List<string>? facilities = null)
        {
            var query = BaseQuery().Where(r => r.Status == RoomStatus.Active);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.Title.Contains(search) || r.Address.Contains(search) || r.District.Contains(search));
            if (!string.IsNullOrWhiteSpace(district))
                query = query.Where(r => r.District == district);
            if (!string.IsNullOrWhiteSpace(thana))
                query = query.Where(r => r.Thana == thana);
            if (roomType.HasValue)
                query = query.Where(r => r.RoomType == roomType.Value);
            if (minRent.HasValue)
                query = query.Where(r => r.MonthlyRent >= minRent.Value);
            if (maxRent.HasValue)
                query = query.Where(r => r.MonthlyRent <= maxRent.Value);
            if (availableNow == true)
                query = query.Where(r => r.IsAvailable);
            if (facilities != null && facilities.Any())
                query = query.Where(r => r.Facilities.Any(f => facilities.Contains(f.FacilityName)));

            query = sortBy switch
            {
                "rent_asc"  => query.OrderBy(r => r.MonthlyRent),
                "rent_desc" => query.OrderByDescending(r => r.MonthlyRent),
                "views"     => query.OrderByDescending(r => r.ViewCount),
                _           => query.OrderByDescending(r => r.PostedDate)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<Room> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
        }
    }
}
