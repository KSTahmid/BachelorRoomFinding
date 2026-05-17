using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class LoginHistoryRepository : IRepository<LoginHistory>
    {
        private readonly AppDbContext _context;
        public LoginHistoryRepository(AppDbContext context) => _context = context;

        public async Task<PagedResult<LoginHistory>> GetPagedAsync(int pageNumber = 1, int pageSize = 20, string? search = null)
        {
            var query = _context.LoginHistories.Include(l => l.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l => l.User.UserName.Contains(search) || l.IpAddress!.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(l => l.LoginAt)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<LoginHistory> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
        }

        public async Task<LoginHistory?> GetByIdAsync(int id) => await _context.LoginHistories.FindAsync(id);
        public async Task<IEnumerable<LoginHistory>> GetAllAsync() => await _context.LoginHistories.Include(l => l.User).OrderByDescending(l => l.LoginAt).ToListAsync();
        public async Task AddAsync(LoginHistory entity) { _context.LoginHistories.Add(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(LoginHistory entity) { _context.LoginHistories.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(int id) { var e = await _context.LoginHistories.FindAsync(id); if (e != null) { _context.LoginHistories.Remove(e); await _context.SaveChangesAsync(); } }
        public async Task<bool> ExistsAsync(int id) => await _context.LoginHistories.AnyAsync(l => l.Id == id);
    }
}
