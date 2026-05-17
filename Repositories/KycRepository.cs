using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class KycRepository : IRepository<KycDocument>
    {
        private readonly AppDbContext _context;
        public KycRepository(AppDbContext context) => _context = context;

        public async Task<PagedResult<KycDocument>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.KycDocuments.Include(k => k.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(k => k.NationalIdNumber.Contains(search) || k.User.UserName.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(k => k.SubmittedAt)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<KycDocument> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
        }

        public async Task<KycDocument?> GetByIdAsync(int id) =>
            await _context.KycDocuments.Include(k => k.User).FirstOrDefaultAsync(k => k.Id == id);

        public async Task<IEnumerable<KycDocument>> GetAllAsync() =>
            await _context.KycDocuments.Include(k => k.User).OrderByDescending(k => k.SubmittedAt).ToListAsync();

        public async Task AddAsync(KycDocument entity) { _context.KycDocuments.Add(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(KycDocument entity) { _context.KycDocuments.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(int id) { var e = await _context.KycDocuments.FindAsync(id); if (e != null) { _context.KycDocuments.Remove(e); await _context.SaveChangesAsync(); } }
        public async Task<bool> ExistsAsync(int id) => await _context.KycDocuments.AnyAsync(k => k.Id == id);
    }
}
