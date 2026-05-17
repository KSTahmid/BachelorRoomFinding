using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Filters;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Controllers
{
    [RequireRole("Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRepository<User> _userRepo;
        private readonly IRoomRepository _roomRepo;
        private readonly IRepository<KycDocument> _kycRepo;
        private readonly IRepository<LoginHistory> _loginHistRepo;
        private readonly IRepository<RentalApplication> _appRepo;
        private readonly NotificationService _notifSvc;

        public AdminController(AppDbContext context, IRepository<User> userRepo,
            IRoomRepository roomRepo, IRepository<KycDocument> kycRepo,
            IRepository<LoginHistory> loginHistRepo, IRepository<RentalApplication> appRepo,
            NotificationService notifSvc)
        {
            _context      = context;
            _userRepo     = userRepo;
            _roomRepo     = roomRepo;
            _kycRepo      = kycRepo;
            _loginHistRepo = loginHistRepo;
            _appRepo      = appRepo;
            _notifSvc     = notifSvc;
        }

        // ── Dashboard ────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers    = await _context.Users.CountAsync(u => u.Role.RoleName == "User");
            ViewBag.TotalOwners   = await _context.Users.CountAsync(u => u.Role.RoleName == "Owner");
            ViewBag.TotalRooms    = await _context.Rooms.CountAsync();
            ViewBag.ActiveRooms   = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Active);
            ViewBag.PendingRooms  = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.PendingApproval);
            ViewBag.PendingKyc    = await _context.KycDocuments.CountAsync(k => k.Status == KycStatus.Pending);
            ViewBag.TotalApps     = await _context.RentalApplications.CountAsync();

            // Chart data: registrations per month (last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            ViewBag.RegData = await _context.Users
                .Where(u => u.CreatedAt >= sixMonthsAgo)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // Application status breakdown for pie chart
            ViewBag.AppPending  = await _context.RentalApplications.CountAsync(a => a.Status == ApplicationStatus.Pending);
            ViewBag.AppApproved = await _context.RentalApplications.CountAsync(a => a.Status == ApplicationStatus.Approved);
            ViewBag.AppRejected = await _context.RentalApplications.CountAsync(a => a.Status == ApplicationStatus.Rejected);

            return View();
        }

        // ── User Management ───────────────────────────────────────────
        public async Task<IActionResult> Users(int page = 1, string search = "")
        {
            var result = await _userRepo.GetPagedAsync(page, 15, search);
            ViewBag.Search = search;
            return View(result);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            user.IsApprovedByAdmin = true;
            user.AccountStatus     = AccountStatus.Active;
            await _userRepo.UpdateAsync(user);
            await _notifSvc.CreateAsync(user.UserId, "Account Approved",
                "Your account has been approved by an administrator. You can now access all features.",
                NotificationType.General);
            TempData["Success"] = $"{user.UserName} approved.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            user.AccountStatus = AccountStatus.Suspended;
            await _userRepo.UpdateAsync(user);
            TempData["Success"] = $"{user.UserName} suspended.";
            return RedirectToAction(nameof(Users));
        }

        // ── Room Management ───────────────────────────────────────────
        public async Task<IActionResult> Rooms(int page = 1, string search = "")
        {
            var result = await _roomRepo.GetPagedAsync(page, 15, search);
            ViewBag.Search = search;
            return View(result);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRoom(int id)
        {
            var room = await _roomRepo.GetByIdAsync(id);
            if (room == null) return NotFound();
            room.Status = RoomStatus.Active;
            await _roomRepo.UpdateAsync(room);
            await _notifSvc.CreateAsync(room.OwnerId, "Room Approved",
                $"Your room listing \"{room.Title}\" has been approved and is now live.",
                NotificationType.General);
            TempData["Success"] = "Room approved and listed.";
            return RedirectToAction(nameof(Rooms));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRoom(int id)
        {
            var room = await _roomRepo.GetByIdAsync(id);
            if (room == null) return NotFound();
            room.Status = RoomStatus.Inactive;
            await _roomRepo.UpdateAsync(room);
            await _notifSvc.CreateAsync(room.OwnerId, "Room Rejected",
                $"Your room listing \"{room.Title}\" was not approved. Please review our guidelines.",
                NotificationType.General);
            TempData["Success"] = "Room rejected.";
            return RedirectToAction(nameof(Rooms));
        }

        // ── KYC Management ────────────────────────────────────────────
        public async Task<IActionResult> Kyc(int page = 1)
        {
            var result = await _kycRepo.GetPagedAsync(page, 15);
            return View(result);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveKyc(int id)
        {
            var kyc = await _kycRepo.GetByIdAsync(id);
            if (kyc == null) return NotFound();

            var adminId = HttpContext.Session.GetInt32("UserId")!.Value;
            kyc.Status           = KycStatus.Approved;
            kyc.ReviewedAt       = DateTime.Now;
            kyc.ReviewedByUserId = adminId;
            await _kycRepo.UpdateAsync(kyc);

            // Mark user as verified
            var user = await _userRepo.GetByIdAsync(kyc.UserId);
            if (user != null) { user.IsVerified = true; await _userRepo.UpdateAsync(user); }

            await _notifSvc.CreateAsync(kyc.UserId, "KYC Approved",
                "Your identity verification has been approved. You now have a verified badge.",
                NotificationType.KycApproval);
            TempData["Success"] = "KYC approved.";
            return RedirectToAction(nameof(Kyc));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectKyc(int id, string note)
        {
            var kyc = await _kycRepo.GetByIdAsync(id);
            if (kyc == null) return NotFound();

            var adminId = HttpContext.Session.GetInt32("UserId")!.Value;
            kyc.Status           = KycStatus.Rejected;
            kyc.ReviewedAt       = DateTime.Now;
            kyc.ReviewedByUserId = adminId;
            kyc.ReviewNote       = note;
            await _kycRepo.UpdateAsync(kyc);

            await _notifSvc.CreateAsync(kyc.UserId, "KYC Rejected",
                $"Your KYC was rejected. Reason: {note}. Please re-submit with correct documents.",
                NotificationType.KycApproval);
            TempData["Success"] = "KYC rejected.";
            return RedirectToAction(nameof(Kyc));
        }

        public async Task<IActionResult> LoginHistory(int page = 1, string search = "")
        {
            var result = await _loginHistRepo.GetPagedAsync(page, 20, search);
            ViewBag.Search = search;
            return View(result);
        }

        // ── Reports Management ────────────────────────────────────────
        public async Task<IActionResult> Reports(int page = 1)
        {
            var reports = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.TargetRoom)
                .Include(r => r.TargetUser)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * 15).Take(15).ToListAsync();
            return View(reports);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int id, string note)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();
            report.Status = ReportStatus.Resolved;
            report.AdminNote = note;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Report marked as resolved.";
            return RedirectToAction(nameof(Reports));
        }
    }
}
