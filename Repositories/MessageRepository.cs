using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class MessageRepository : IRepository<Message>
    {
        private readonly AppDbContext _context;
        public MessageRepository(AppDbContext context) => _context = context;

        public async Task<PagedResult<Message>> GetPagedAsync(int pageNumber = 1, int pageSize = 20, string? search = null)
        {
            var query = _context.Messages.Include(m => m.Sender).Include(m => m.Receiver).AsQueryable();
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(m => m.SentAt)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<Message> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
        }

        public async Task<Message?> GetByIdAsync(int id) =>
            await _context.Messages.Include(m => m.Sender).Include(m => m.Receiver).FirstOrDefaultAsync(m => m.Id == id);

        public async Task<IEnumerable<Message>> GetAllAsync() =>
            await _context.Messages.Include(m => m.Sender).Include(m => m.Receiver)
                .OrderByDescending(m => m.SentAt).ToListAsync();

        public async Task<IEnumerable<Message>> GetConversationAsync(int userId1, int userId2) =>
            await _context.Messages
                .Include(m => m.Sender).Include(m => m.Receiver)
                .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                             (m.SenderId == userId2 && m.ReceiverId == userId1))
                .OrderBy(m => m.SentAt).ToListAsync();

        public async Task AddAsync(Message entity) { _context.Messages.Add(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Message entity) { _context.Messages.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(int id) { var e = await _context.Messages.FindAsync(id); if (e != null) { _context.Messages.Remove(e); await _context.SaveChangesAsync(); } }
        public async Task<bool> ExistsAsync(int id) => await _context.Messages.AnyAsync(m => m.Id == id);
    }
}
