using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class NotificationRepository : IRepository<Notification>
    {
        private readonly AppDbContext _context;
        public NotificationRepository(AppDbContext context) => _context = context;

        public async Task<PagedResult<Notification>> GetPagedAsync(int pageNumber = 1, int pageSize = 20, string? search = null)
        {
            var query = _context.Notifications.AsQueryable();
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<Notification> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
        }

        public async Task<Notification?> GetByIdAsync(int id) => await _context.Notifications.FindAsync(id);
        public async Task<IEnumerable<Notification>> GetAllAsync() => await _context.Notifications.OrderByDescending(n => n.CreatedAt).ToListAsync();
        public async Task AddAsync(Notification entity) { _context.Notifications.Add(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Notification entity) { _context.Notifications.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(int id) { var e = await _context.Notifications.FindAsync(id); if (e != null) { _context.Notifications.Remove(e); await _context.SaveChangesAsync(); } }
        public async Task<bool> ExistsAsync(int id) => await _context.Notifications.AnyAsync(n => n.Id == id);
    }
}
