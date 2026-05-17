using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class SavedRoomRepository : IRepository<SavedRoom>
    {
        private readonly AppDbContext _context;
        public SavedRoomRepository(AppDbContext context) => _context = context;

        public async Task<PagedResult<SavedRoom>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.SavedRooms.Include(s => s.Room).ThenInclude(r => r.Photos)
                .Include(s => s.User).AsQueryable();
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(s => s.SavedAt)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<SavedRoom> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
        }

        public async Task<SavedRoom?> GetByIdAsync(int id) => await _context.SavedRooms.FindAsync(id);
        public async Task<IEnumerable<SavedRoom>> GetAllAsync() => await _context.SavedRooms.Include(s => s.Room).ToListAsync();
        public async Task AddAsync(SavedRoom entity) { _context.SavedRooms.Add(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(SavedRoom entity) { _context.SavedRooms.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(int id) { var e = await _context.SavedRooms.FindAsync(id); if (e != null) { _context.SavedRooms.Remove(e); await _context.SaveChangesAsync(); } }
        public async Task<bool> ExistsAsync(int id) => await _context.SavedRooms.AnyAsync(s => s.Id == id);
    }
}
