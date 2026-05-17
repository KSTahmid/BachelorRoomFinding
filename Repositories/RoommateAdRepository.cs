using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class RoommateAdRepository : IRepository<RoommateAd>
    {
        private readonly AppDbContext _context;
        public RoommateAdRepository(AppDbContext context) => _context = context;

        public async Task<RoommateAd?> GetByIdAsync(int id) => await _context.RoommateAds.FindAsync(id);

        public async Task<IEnumerable<RoommateAd>> GetAllAsync() => await _context.RoommateAds.ToListAsync();

        public async Task<PagedResult<RoommateAd>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.RoommateAds.AsQueryable();
            if (!string.IsNullOrEmpty(search)) query = query.Where(a => a.PreferredAreas.Contains(search) || a.Description.Contains(search));
            
            var total = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<RoommateAd> { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
        }

        public async Task AddAsync(RoommateAd entity) { _context.RoommateAds.Add(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(RoommateAd entity) { _context.RoommateAds.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(int id) { var ad = await GetByIdAsync(id); if (ad != null) { _context.RoommateAds.Remove(ad); await _context.SaveChangesAsync(); } }
        public async Task<bool> ExistsAsync(int id) => await _context.RoommateAds.AnyAsync(a => a.Id == id);
    }
}
