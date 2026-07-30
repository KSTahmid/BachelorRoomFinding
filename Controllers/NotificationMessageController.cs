using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Filters;
using BachelorRoomFinding.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Controllers
{
    [RequireLogin]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context) => _context = context;

        private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

        public async Task<IActionResult> Index(int page = 1)
        {
            var uid  = UserId;
            var total = await _context.Notifications.CountAsync(n => n.UserId == uid);
            var items = await _context.Notifications
                .Where(n => n.UserId == uid)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * 20).Take(20).ToListAsync();

            // Mark all fetched as read
            foreach (var n in items.Where(n => !n.IsRead))
                n.IsRead = true;
            await _context.SaveChangesAsync();

            ViewBag.Page  = page;
            ViewBag.Total = total;
            return View(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var uid   = UserId;
            var items = await _context.Notifications
                .Where(n => n.UserId == uid && !n.IsRead).ToListAsync();
            foreach (var n in items) n.IsRead = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var uid   = HttpContext.Session.GetInt32("UserId");
            if (uid == null) return Json(new { count = 0 });
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == uid.Value && !n.IsRead);
            return Json(new { count });
        }
    }

    [RequireLogin]
    public class MessageController : Controller
    {
        private readonly BachelorRoomFinding.Interfaces.IMessageRepository _msgRepo;
        private readonly BachelorRoomFinding.Interfaces.IRepository<BachelorRoomFinding.Entities.User> _userRepo;
        private readonly BachelorRoomFinding.Services.NotificationService _notifSvc;
        private readonly BachelorRoomFinding.Services.EmailService _emailSvc;
        private readonly AppDbContext _context;

        public MessageController(
            BachelorRoomFinding.Interfaces.IMessageRepository msgRepo,
            BachelorRoomFinding.Interfaces.IRepository<BachelorRoomFinding.Entities.User> userRepo,
            BachelorRoomFinding.Services.NotificationService notifSvc,
            BachelorRoomFinding.Services.EmailService emailSvc,
            AppDbContext context)
        {
            _msgRepo  = msgRepo;
            _userRepo = userRepo;
            _notifSvc = notifSvc;
            _emailSvc = emailSvc;
            _context  = context;
        }

        private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

        public async Task<IActionResult> Inbox()
        {
            var uid = UserId;
            // Get distinct conversation partners
            var partners = await _context.Messages
                .Where(m => m.SenderId == uid || m.ReceiverId == uid)
                .Select(m => m.SenderId == uid ? m.ReceiverId : m.SenderId)
                .Distinct().ToListAsync();

            var users = await _context.Users
                .Where(u => partners.Contains(u.UserId)).ToListAsync();

            var inbox = new List<(User user, Message lastMsg, int unread)>();
            foreach (var u in users)
            {
                var last = await _context.Messages
                    .Where(m => (m.SenderId == uid && m.ReceiverId == u.UserId) ||
                                 (m.SenderId == u.UserId && m.ReceiverId == uid))
                    .OrderByDescending(m => m.SentAt).FirstOrDefaultAsync();
                var unread = await _context.Messages
                    .CountAsync(m => m.SenderId == u.UserId && m.ReceiverId == uid && !m.IsRead);
                if (last != null) inbox.Add((u, last, unread));
            }
            ViewBag.Inbox = inbox.OrderByDescending(x => x.lastMsg.SentAt).ToList();
            return View();
        }

        public async Task<IActionResult> Conversation(int userId)
        {
            var uid      = UserId;
            var other    = await _userRepo.GetByIdAsync(userId);
            if (other == null) return NotFound();

            var messages = await _msgRepo.GetConversationAsync(uid, userId);

            // Mark received as read
            var unread = messages.Where(m => m.ReceiverId == uid && !m.IsRead).ToList();
            foreach (var m in unread) m.IsRead = true;
            if (unread.Any()) await _context.SaveChangesAsync();

            ViewBag.OtherUser = other;
            ViewBag.CurrentUserId = uid;
            return View(messages);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int receiverId, string content, int? roomId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction(nameof(Conversation), new { userId = receiverId });

            var uid = UserId;
            await _msgRepo.AddAsync(new Message
            {
                SenderId   = uid,
                ReceiverId = receiverId,
                RoomId     = roomId,
                Content    = content.Trim(),
                SentAt     = DateTime.Now,
                IsRead     = false
            });

            var senderName = HttpContext.Session.GetString("UserName");
            await _notifSvc.CreateAsync(receiverId,
                $"New message from {senderName}",
                content.Length > 80 ? content[..80] + "..." : content,
                NotificationType.NewMessage);

            // Send real email notification to the receiver
            var receiver = await _userRepo.GetByIdAsync(receiverId);
            if (receiver != null && !string.IsNullOrEmpty(receiver.Email))
            {
                await _emailSvc.SendAsync(
                    receiver.Email,
                    $"New message from {senderName} - MessBasha",
                    $"Hi {receiver.UserName},<br><br>You have received a new message from <strong>{senderName}</strong>:<br><br>\"{content.Trim()}\"<br><br>Please log in to the portal to reply.");
            }

            return RedirectToAction(nameof(Conversation), new { userId = receiverId });
        }
    }
}
