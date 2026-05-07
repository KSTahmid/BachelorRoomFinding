using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class RoomRepository : IRepository<Room>
    {
        private readonly AppDbContext _context;

        public RoomRepository(AppDbContext context) => _context = context;

        public async Task<PagedResult<Room>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.Rooms.Include(r => r.Owner).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.Title.Contains(search) ||
                                        r.Address.Contains(search) ||
                                        r.Description.Contains(search));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.PostedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Room>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Room?> GetByIdAsync(int id) =>
            await _context.Rooms.Include(r => r.Owner).FirstOrDefaultAsync(r => r.Id == id);

        public async Task<IEnumerable<Room>> GetAllAsync() =>
            await _context.Rooms.Include(r => r.Owner).OrderByDescending(r => r.PostedDate).ToListAsync();

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
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Rooms.AnyAsync(r => r.Id == id);
    }
}
