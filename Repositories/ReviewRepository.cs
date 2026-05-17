using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class ReviewRepository : IRepository<Review>
    {
        private readonly AppDbContext _context;
        public ReviewRepository(AppDbContext context) => _context = context;

        public async Task<PagedResult<Review>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.Reviews.Include(r => r.Reviewer).Include(r => r.Room).AsQueryable();
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<Review> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
        }

        public async Task<Review?> GetByIdAsync(int id) =>
            await _context.Reviews.Include(r => r.Reviewer).Include(r => r.Room).FirstOrDefaultAsync(r => r.Id == id);

        public async Task<IEnumerable<Review>> GetAllAsync() =>
            await _context.Reviews.Include(r => r.Reviewer).OrderByDescending(r => r.CreatedAt).ToListAsync();

        public async Task AddAsync(Review entity) { _context.Reviews.Add(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Review entity) { _context.Reviews.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(int id) { var e = await _context.Reviews.FindAsync(id); if (e != null) { _context.Reviews.Remove(e); await _context.SaveChangesAsync(); } }
        public async Task<bool> ExistsAsync(int id) => await _context.Reviews.AnyAsync(r => r.Id == id);
    }
}
