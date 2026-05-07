using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Models;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Repositories
{
    public class RoleRepository : IRepository<Role>
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context) => _context = context;

        public async Task<PagedResult<Role>> GetPagedAsync(int pageNumber = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.Roles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.RoleName.Contains(search) ||
                                        (r.RoleDescription != null && r.RoleDescription.Contains(search)));

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(r => r.RoleName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Role>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<Role?> GetByIdAsync(int id) =>
            await _context.Roles.FindAsync(id);

        public async Task<IEnumerable<Role>> GetAllAsync() =>
            await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();

        public async Task AddAsync(Role entity)
        {
            _context.Roles.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Role entity)
        {
            _context.Roles.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role != null)
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Roles.AnyAsync(r => r.Id == id);
    }
}
