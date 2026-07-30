using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class PaymentRepository : IRepository<Payment>
    {
        private readonly AppDbContext _context;
        public PaymentRepository(AppDbContext context) => _context = context;

        public async Task<PagedResult<Payment>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.Payments.Include(p => p.Application).ThenInclude(a => a.Room).AsQueryable();
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<Payment> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
        }

        public async Task<Payment?> GetByIdAsync(int id) =>
            await _context.Payments.Include(p => p.Application).ThenInclude(a => a.Applicant)
                .Include(p => p.Application).ThenInclude(a => a.Room)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IEnumerable<Payment>> GetAllAsync() =>
            await _context.Payments.Include(p => p.Application).ToListAsync();

        public async Task AddAsync(Payment entity) { _context.Payments.Add(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Payment entity) { _context.Payments.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(int id) { var e = await _context.Payments.FindAsync(id); if (e != null) { _context.Payments.Remove(e); await _context.SaveChangesAsync(); } }
        public async Task<bool> ExistsAsync(int id) => await _context.Payments.AnyAsync(p => p.Id == id);
    }
}
