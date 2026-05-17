using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;

namespace BachelorRoomFinding.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context) => _context = context;

        public async Task CreateAsync(int userId, string title, string message,
            NotificationType type = NotificationType.General)
        {
            _context.Notifications.Add(new Notification
            {
                UserId              = userId,
                Title               = title,
                NotificationMessage = message,
                Type                = type,
                IsRead              = false,
                CreatedAt           = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await System.Threading.Tasks.Task.FromResult(
                _context.Notifications.Count(n => n.UserId == userId && !n.IsRead));
        }
    }
}
