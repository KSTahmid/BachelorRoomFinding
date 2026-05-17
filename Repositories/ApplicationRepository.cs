using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class ApplicationRepository : IRepository<RentalApplication>
    {
        private readonly AppDbContext _context;
        public ApplicationRepository(AppDbContext context) => _context = context;

        private IQueryable<RentalApplication> BaseQuery() =>
            _context.RentalApplications
                .Include(a => a.Room).ThenInclude(r => r.Photos)
                .Include(a => a.Applicant)
                .Include(a => a.Payment);

        public async Task<PagedResult<RentalApplication>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, string? search = null)
        {
            var query = BaseQuery();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.Room.Title.Contains(search) || a.Applicant.UserName.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.AppliedAt)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<RentalApplication> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
        }

        public async Task<RentalApplication?> GetByIdAsync(int id) =>
            await BaseQuery().FirstOrDefaultAsync(a => a.Id == id);

        public async Task<IEnumerable<RentalApplication>> GetAllAsync() =>
            await BaseQuery().OrderByDescending(a => a.AppliedAt).ToListAsync();

        public async Task AddAsync(RentalApplication entity) { _context.RentalApplications.Add(entity); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(RentalApplication entity) { _context.RentalApplications.Update(entity); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(int id) { var e = await _context.RentalApplications.FindAsync(id); if (e != null) { _context.RentalApplications.Remove(e); await _context.SaveChangesAsync(); } }
        public async Task<bool> ExistsAsync(int id) => await _context.RentalApplications.AnyAsync(a => a.Id == id);
    }
}
