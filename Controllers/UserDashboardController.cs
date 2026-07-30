using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Filters;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Services;
using BachelorRoomFinding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Controllers
{
    [RequireLogin]
    public class UserDashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRoomRepository _roomRepo;
        private readonly IRepository<RentalApplication> _appRepo;
        private readonly IRepository<SavedRoom> _savedRepo;
        private readonly IRepository<Review> _reviewRepo;
        private readonly IRepository<KycDocument> _kycRepo;
        private readonly IRepository<Payment> _paymentRepo;
        private readonly FileUploadService _fileSvc;
        private readonly NotificationService _notifSvc;

        public UserDashboardController(AppDbContext context, IRoomRepository roomRepo,
            IRepository<RentalApplication> appRepo, IRepository<SavedRoom> savedRepo,
            IRepository<Review> reviewRepo, IRepository<KycDocument> kycRepo,
            IRepository<Payment> paymentRepo, FileUploadService fileSvc,
            NotificationService notifSvc)
        {
            _context     = context;
            _roomRepo    = roomRepo;
            _appRepo     = appRepo;
            _savedRepo   = savedRepo;
            _reviewRepo  = reviewRepo;
            _kycRepo     = kycRepo;
            _paymentRepo = paymentRepo;
            _fileSvc     = fileSvc;
            _notifSvc    = notifSvc;
        }

        private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

        // ── Dashboard ────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var uid = UserId;
            ViewBag.MyApplications = await _context.RentalApplications
                .Include(a => a.Room).ThenInclude(r => r.Photos)
                .Where(a => a.ApplicantId == uid)
                .OrderByDescending(a => a.AppliedAt).Take(5).ToListAsync();

            ViewBag.SavedCount   = await _context.SavedRooms.CountAsync(s => s.UserId == uid);
            ViewBag.ApprovedCount = await _context.RentalApplications
                .CountAsync(a => a.ApplicantId == uid && a.Status == ApplicationStatus.Approved);

            var kyc = await _context.KycDocuments.FirstOrDefaultAsync(k => k.UserId == uid);
            ViewBag.KycStatus = kyc?.Status.ToString() ?? "Not Submitted";

            return View();
        }

        // ── Browse Rooms ─────────────────────────────────────────────
        [AllowAnonymousAccess]
        public async Task<IActionResult> Browse(RoomFilterViewModel filter)
        {
            var result = await _roomRepo.GetFilteredAsync(
                filter.Page, filter.PageSize,
                filter.Search, filter.District, filter.Thana,
                filter.RoomType, filter.MinRent, filter.MaxRent,
                filter.AvailableNow, filter.SortBy, filter.Facilities);

            ViewBag.Filter = filter;

            // Saved room IDs for current user
            if (HttpContext.Session.GetInt32("UserId") is int uid)
            {
                var saved = await _context.SavedRooms.Where(s => s.UserId == uid)
                    .Select(s => s.RoomId).ToListAsync();
                ViewBag.SavedIds = saved;
            }
            else ViewBag.SavedIds = new List<int>();

            return View(result);
        }

        // ── Room Detail ───────────────────────────────────────────────
        [AllowAnonymousAccess]
        public async Task<IActionResult> RoomDetail(int id)
        {
            var room = await _roomRepo.GetByIdAsync(id);
            if (room == null || room.Status != RoomStatus.Active) return NotFound();

            // Track view
            var sessionId = HttpContext.Session.Id;
            var uid = HttpContext.Session.GetInt32("UserId");
            var alreadyViewed = await _context.RoomViews.AnyAsync(v =>
                v.RoomId == id &&
                (uid.HasValue ? v.ViewerUserId == uid : v.SessionId == sessionId) &&
                v.ViewedAt > DateTime.Now.AddHours(-1));

            if (!alreadyViewed)
            {
                _context.RoomViews.Add(new RoomView
                    { RoomId = id, ViewerUserId = uid, SessionId = sessionId, ViewedAt = DateTime.Now });
                room.ViewCount++;
                _context.Rooms.Update(room);
                await _context.SaveChangesAsync();
            }

            var avgRating = room.Reviews.Any() ? room.Reviews.Average(r => r.Rating) : 0;
            ViewBag.AvgRating = Math.Round(avgRating, 1);

            bool isSaved = uid.HasValue &&
                await _context.SavedRooms.AnyAsync(s => s.UserId == uid.Value && s.RoomId == id);
            ViewBag.IsSaved = isSaved;

            bool alreadyApplied = uid.HasValue &&
                await _context.RentalApplications.AnyAsync(a =>
                    a.ApplicantId == uid.Value && a.RoomId == id &&
                    a.Status != ApplicationStatus.Cancelled);
            ViewBag.AlreadyApplied = alreadyApplied;
            ViewBag.CanReview = uid.HasValue && await CanReviewRoomAsync(id, uid.Value);
            ViewBag.OwnerBkash = room.Owner?.BkashNumber ?? room.Owner?.PhoneNumber;
            ViewBag.OwnerNagad = room.Owner?.NagadNumber ?? room.Owner?.PhoneNumber;
            ViewBag.IsDemoNumber = room.Owner?.IsDemoNumber ?? true;

            return View(room);
        }

        // ── Apply ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Apply(int roomId)
        {
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null) return NotFound();
            if (room.OwnerId == UserId)
            {
                TempData["Error"] = "You cannot apply to your own room.";
                return RedirectToAction(nameof(RoomDetail), new { id = roomId });
            }
            ViewBag.Room = room;
            return View(new ApplicationViewModel { RoomId = roomId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplicationViewModel vm)
        {
            var uid = UserId;
            var room = await _roomRepo.GetByIdAsync(vm.RoomId);
            
            if (room != null && room.OwnerId == uid)
            {
                TempData["Error"] = "You cannot apply to your own room.";
                return RedirectToAction(nameof(RoomDetail), new { id = vm.RoomId });
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Room = room;
                return View(vm);
            }
            var alreadyApplied = await _context.RentalApplications.AnyAsync(a =>
                a.ApplicantId == uid && a.RoomId == vm.RoomId &&
                a.Status != ApplicationStatus.Cancelled);

            if (alreadyApplied)
            {
                TempData["Error"] = "You have already applied for this room.";
                return RedirectToAction(nameof(RoomDetail), new { id = vm.RoomId });
            }

            var app = new RentalApplication
            {
                RoomId        = vm.RoomId,
                ApplicantId   = uid,
                MoveInDate    = vm.MoveInDate,
                DurationMonths = vm.DurationMonths,
                Message       = vm.Message,
                Status        = ApplicationStatus.Pending,
                AppliedAt     = DateTime.Now
            };
            await _appRepo.AddAsync(app);

            // Notify owner
            if (room != null)
                await _notifSvc.CreateAsync(room.OwnerId, "New Rental Application",
                    $"Someone has applied for your room \"{room.Title}\".",
                    NotificationType.ApplicationStatus);

            TempData["Success"] = "Application submitted successfully!";
            return RedirectToAction(nameof(Dashboard));
        }

        // ── Payment ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Payment(int applicationId)
        {
            var app = await _context.RentalApplications
                .Include(a => a.Room)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null || app.ApplicantId != UserId) return Forbid();
            ViewBag.Application = app;
            return View(new PaymentViewModel { ApplicationId = applicationId, Amount = app.Room.MonthlyRent });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(PaymentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Application = await _appRepo.GetByIdAsync(vm.ApplicationId);
                return View(vm);
            }

            // Guard: reject duplicate bKash/Nagad transaction codes
            if (!string.IsNullOrWhiteSpace(vm.TransactionId))
            {
                var txExists = await _context.Payments
                    .AnyAsync(p => p.TransactionId == vm.TransactionId);
                if (txExists)
                {
                    ModelState.AddModelError("TransactionId",
                        "This transaction ID has already been submitted. Each bKash/Nagad transaction can only be used once.");
                    ViewBag.Application = await _context.RentalApplications
                        .Include(a => a.Room)
                        .FirstOrDefaultAsync(a => a.Id == vm.ApplicationId);
                    return View(vm);
                }
            }

            var payment = new Payment
            {
                ApplicationId  = vm.ApplicationId,
                Method         = vm.Method,
                Amount         = vm.Amount,
                TransactionId  = vm.TransactionId,
                BankName       = vm.BankName,
                BankAccount    = vm.BankAccount,
                Status         = "Pending",
                CreatedAt      = DateTime.Now
            };
            await _paymentRepo.AddAsync(payment);

            var app = await _context.RentalApplications
                .Include(a => a.Room)
                .FirstOrDefaultAsync(a => a.Id == vm.ApplicationId);

            if (app != null)
                await _notifSvc.CreateAsync(app.Room.OwnerId, "Payment Submitted",
                    $"Payment received for \"{app.Room.Title}\". Please verify.",
                    NotificationType.PaymentConfirmed);

            TempData["Success"] = "Payment submitted! Waiting for owner confirmation.";
            return RedirectToAction(nameof(Dashboard));
        }

        // ── Save / Unsave ─────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRoom(int roomId)
        {
            var uid = UserId;
            var exists = await _context.SavedRooms.AnyAsync(s => s.UserId == uid && s.RoomId == roomId);
            if (!exists)
            {
                _context.SavedRooms.Add(new SavedRoom { UserId = uid, RoomId = roomId, SavedAt = DateTime.Now });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Room saved!";
            }
            return RedirectToAction(nameof(RoomDetail), new { id = roomId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnsaveRoom(int roomId)
        {
            var uid  = UserId;
            var saved = await _context.SavedRooms.FirstOrDefaultAsync(s => s.UserId == uid && s.RoomId == roomId);
            if (saved != null) { _context.SavedRooms.Remove(saved); await _context.SaveChangesAsync(); }
            TempData["Success"] = "Room removed from saved.";
            return RedirectToAction(nameof(RoomDetail), new { id = roomId });
        }

        // ── Saved Rooms ───────────────────────────────────────────────
        public async Task<IActionResult> SavedRooms()
        {
            var saved = await _context.SavedRooms
                .Include(s => s.Room).ThenInclude(r => r.Photos)
                .Include(s => s.Room).ThenInclude(r => r.Facilities)
                .Where(s => s.UserId == UserId)
                .OrderByDescending(s => s.SavedAt).ToListAsync();
            return View(saved);
        }

        // ── Review ────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Review(int roomId)
        {
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null) return NotFound();
            var canReview = await CanReviewRoomAsync(roomId, UserId);
            if (!canReview)
            {
                TempData["Error"] = "Only confirmed tenants who completed rent payment can review this room.";
                return RedirectToAction(nameof(RoomDetail), new { id = roomId });
            }
            ViewBag.Room = room;
            return View(new ReviewViewModel { RoomId = roomId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(ReviewViewModel vm)
        {
            if (!ModelState.IsValid) { ViewBag.Room = await _roomRepo.GetByIdAsync(vm.RoomId); return View(vm); }

            var uid = UserId;
            if (!await CanReviewRoomAsync(vm.RoomId, uid))
            {
                TempData["Error"] = "Only confirmed tenants who completed rent payment can review this room.";
                return RedirectToAction(nameof(RoomDetail), new { id = vm.RoomId });
            }

            var already = await _context.Reviews.AnyAsync(r => r.RoomId == vm.RoomId && r.ReviewerId == uid);
            if (already) { TempData["Error"] = "You have already reviewed this room."; return RedirectToAction(nameof(RoomDetail), new { id = vm.RoomId }); }

            await _reviewRepo.AddAsync(new Review
            {
                RoomId     = vm.RoomId,
                ReviewerId = uid,
                Rating     = vm.Rating,
                Comment    = vm.Comment,
                CreatedAt  = DateTime.Now,
                IsVerifiedTenantReview = true
            });

            TempData["Success"] = "Review submitted!";
            return RedirectToAction(nameof(RoomDetail), new { id = vm.RoomId });
        }

        // ── KYC ───────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Kyc()
        {
            var existing = await _context.KycDocuments.FirstOrDefaultAsync(k => k.UserId == UserId);
            ViewBag.Existing = existing;
            return View(new KycViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Kyc(KycViewModel vm)
        {
            ModelState.Remove("NidFrontFile");
            ModelState.Remove("NidBackFile");
            ModelState.Remove("FacePhotoFile");
            if (!ModelState.IsValid) return View(vm);

            var uid      = UserId;
            var existing = await _context.KycDocuments.FirstOrDefaultAsync(k => k.UserId == uid);
            var front    = await _fileSvc.UploadAsync(vm.NidFrontFile,  "kyc", uid);
            var back     = await _fileSvc.UploadAsync(vm.NidBackFile,   "kyc", uid);
            var face     = await _fileSvc.UploadAsync(vm.FacePhotoFile, "kyc", uid);

            if (existing != null)
            {
                existing.NationalIdNumber = vm.NationalIdNumber;
                existing.Status = KycStatus.Pending; existing.SubmittedAt = DateTime.Now; existing.ReviewNote = null;
                if (front != null) existing.NidFrontPath  = front;
                if (back  != null) existing.NidBackPath   = back;
                if (face  != null) existing.FacePhotoPath = face;
                _context.KycDocuments.Update(existing);
            }
            else
            {
                _context.KycDocuments.Add(new KycDocument
                {
                    UserId = uid, NationalIdNumber = vm.NationalIdNumber,
                    NidFrontPath = front, NidBackPath = back, FacePhotoPath = face,
                    Status = KycStatus.Pending, SubmittedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "KYC submitted for review.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportRoom(int roomId, ReportReason reason, string details)
        {
            var report = new Report
            {
                ReporterUserId = UserId,
                TargetRoomId = roomId,
                Reason = reason,
                Details = details,
                Status = ReportStatus.New,
                CreatedAt = DateTime.Now
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Report submitted. Our team will review it shortly.";
            return RedirectToAction(nameof(RoomDetail), new { id = roomId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportOwner(int ownerId, int? roomId, ReportReason reason, string details)
        {
            if (ownerId == UserId)
            {
                TempData["Error"] = "You cannot report your own profile.";
                if (roomId.HasValue && roomId.Value > 0) return RedirectToAction(nameof(RoomDetail), new { id = roomId.Value });
                return RedirectToAction(nameof(Browse));
            }

            var targetUser = await _context.Users.FindAsync(ownerId);
            if (targetUser == null) return NotFound("Owner profile not found.");

            int? targetRoomId = (roomId.HasValue && roomId.Value > 0) ? roomId.Value : null;

            _context.Reports.Add(new Report
            {
                ReporterUserId = UserId,
                TargetRoomId = targetRoomId,
                TargetUserId = ownerId,
                Reason = reason,
                Details = details ?? string.Empty,
                Status = ReportStatus.New,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Owner report submitted successfully. Admin panel has been notified.";
            if (targetRoomId.HasValue) return RedirectToAction(nameof(RoomDetail), new { id = targetRoomId.Value });
            return RedirectToAction(nameof(Browse));
        }

        private async Task<bool> CanReviewRoomAsync(int roomId, int userId)
        {
            // 1. Approved application
            var hasApprovedApp = await _context.RentalApplications.AnyAsync(a =>
                a.RoomId == roomId &&
                a.ApplicantId == userId &&
                a.Status == ApplicationStatus.Approved);
            if (hasApprovedApp) return true;

            // 2. Completed payment
            var hasPayment = await _context.Payments.AnyAsync(p =>
                p.RoomId == roomId &&
                p.UserId == userId &&
                (p.Status == "Completed" || p.Status == "Approved"));
            if (hasPayment) return true;

            // 3. Mess member for this room
            var isMessMember = await _context.MessMembers.AnyAsync(m =>
                m.MessGroup != null &&
                m.MessGroup.RoomId == roomId &&
                m.UserId == userId);

            return isMessMember;
        }
    }

    // Helper attribute to skip RequireLogin on public actions
    public class AllowAnonymousAccessAttribute : Attribute { }
}
