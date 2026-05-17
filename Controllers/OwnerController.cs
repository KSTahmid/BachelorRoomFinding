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
    [RequireRole("Owner")]
    public class OwnerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRoomRepository _roomRepo;
        private readonly IRepository<RentalApplication> _appRepo;
        private readonly IRepository<KycDocument> _kycRepo;
        private readonly FileUploadService _fileSvc;
        private readonly NotificationService _notifSvc;

        public OwnerController(AppDbContext context, IRoomRepository roomRepo,
            IRepository<RentalApplication> appRepo, IRepository<KycDocument> kycRepo,
            FileUploadService fileSvc, NotificationService notifSvc)
        {
            _context  = context;
            _roomRepo = roomRepo;
            _appRepo  = appRepo;
            _kycRepo  = kycRepo;
            _fileSvc  = fileSvc;
            _notifSvc = notifSvc;
        }

        private int OwnerId => HttpContext.Session.GetInt32("UserId")!.Value;

        // ── Dashboard ────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var ownerId = OwnerId;
            ViewBag.MyRooms     = await _context.Rooms.CountAsync(r => r.OwnerId == ownerId);
            ViewBag.ActiveRooms = await _context.Rooms.CountAsync(r => r.OwnerId == ownerId && r.Status == RoomStatus.Active);
            ViewBag.PendingApps = await _context.RentalApplications
                .CountAsync(a => a.Room.OwnerId == ownerId && a.Status == ApplicationStatus.Pending);
            ViewBag.TotalViews  = await _context.RoomViews.CountAsync(v => v.Room.OwnerId == ownerId);

            var recentApps = await _context.RentalApplications
                .Include(a => a.Room).Include(a => a.Applicant)
                .Where(a => a.Room.OwnerId == ownerId)
                .OrderByDescending(a => a.AppliedAt).Take(5).ToListAsync();
            ViewBag.RecentApplications = recentApps;

            return View();
        }

        // ── My Rooms ─────────────────────────────────────────────────
        public async Task<IActionResult> MyRooms(int page = 1)
        {
            var rooms = await _roomRepo.GetOwnerRoomsAsync(OwnerId);
            ViewBag.Rooms = rooms;
            return View(rooms);
        }

        // ── Post Room ─────────────────────────────────────────────────
        [HttpGet]
        public IActionResult PostRoom() => View(new RoomCreateViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PostRoom(RoomCreateViewModel vm)
        {
            ModelState.Remove("PhotoFiles");
            if (!ModelState.IsValid) return View(vm);

            try
            {
                var ownerId = OwnerId;
                var room = new Room
                {
                    Title           = vm.Title,
                    Description     = vm.Description,
                    Address         = vm.Address,
                    District        = vm.District,
                    Thana           = vm.Thana,
                    Rent            = vm.Rent,
                    SecurityDeposit = vm.SecurityDeposit,
                    Advance         = vm.Advance,
                    BedroomCount    = vm.BedroomCount,
                    RoomType        = vm.RoomType,
                    AvailableFrom   = vm.AvailableFrom,
                    OwnerId         = ownerId,
                    Status          = RoomStatus.PendingApproval,
                    PostedDate      = DateTime.Now,
                    Rules = BuildRules(vm)
                };

                await _roomRepo.AddAsync(room);

                // Facilities
                foreach (var f in vm.SelectedFacilities)
                    _context.RoomFacilities.Add(new RoomFacility { RoomId = room.Id, FacilityName = f });

                // Photos
                if (vm.PhotoFiles?.Any() == true)
                {
                    bool first = true;
                    foreach (var file in vm.PhotoFiles)
                    {
                        var path = await _fileSvc.UploadAsync(file, "rooms", ownerId);
                        if (path != null)
                        {
                            _context.RoomPhotos.Add(new RoomPhoto { RoomId = room.Id, PhotoPath = path, IsPrimary = first });
                            first = false;
                        }
                    }
                }
                await _context.SaveChangesAsync();

                TempData["Success"] = "Room submitted for admin approval!";
                return RedirectToAction(nameof(MyRooms));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
        }

        // ── Applications ──────────────────────────────────────────────
        public async Task<IActionResult> Applications(int page = 1)
        {
            var apps = await _context.RentalApplications
                .Include(a => a.Room).Include(a => a.Applicant)
                .Include(a => a.Payment)
                .Where(a => a.Room.OwnerId == OwnerId)
                .OrderByDescending(a => a.AppliedAt)
                .Skip((page - 1) * 10).Take(10).ToListAsync();
            ViewBag.Page = page;
            return View(apps);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int id)
        {
            var app = await _appRepo.GetByIdAsync(id);
            if (app == null || app.Room.OwnerId != OwnerId) return Forbid();

            app.Status     = ApplicationStatus.Approved;
            app.ReviewedAt = DateTime.Now;
            await _appRepo.UpdateAsync(app);

            await _notifSvc.CreateAsync(app.ApplicantId,
                "Application Approved!",
                $"Your application for \"{app.Room.Title}\" was approved. Please proceed with payment.",
                NotificationType.ApplicationStatus);

            TempData["Success"] = "Application approved.";
            return RedirectToAction(nameof(Applications));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineApplication(int id)
        {
            var app = await _appRepo.GetByIdAsync(id);
            if (app == null || app.Room.OwnerId != OwnerId) return Forbid();

            app.Status     = ApplicationStatus.Rejected;
            app.ReviewedAt = DateTime.Now;
            await _appRepo.UpdateAsync(app);

            await _notifSvc.CreateAsync(app.ApplicantId,
                "Application Declined",
                $"Your application for \"{app.Room.Title}\" was declined by the owner.",
                NotificationType.ApplicationStatus);

            TempData["Success"] = "Application declined.";
            return RedirectToAction(nameof(Applications));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int applicationId)
        {
            var app = await _context.RentalApplications
                .Include(a => a.Room)
                .Include(a => a.Payment)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null || app.Room.OwnerId != OwnerId) return Forbid();
            if (app.Payment == null) return NotFound("No payment found.");

            app.Payment.Status = PaymentStatus.Confirmed;
            app.Payment.ConfirmedByUserId = OwnerId;
            
            // Mark room as rented? Or just application as settled?
            // Usually we mark the room as occupied once payment is confirmed.
            app.Room.IsAvailable = false;
            app.Room.Status = RoomStatus.Rented;

            await _context.SaveChangesAsync();

            await _notifSvc.CreateAsync(app.ApplicantId,
                "Payment Confirmed!",
                $"Your payment for \"{app.Room.Title}\" has been confirmed. Welcome to your new room!",
                NotificationType.PaymentConfirmed);

            TempData["Success"] = "Payment confirmed and room marked as Rented.";
            return RedirectToAction(nameof(Applications));
        }

        // ── KYC ───────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Kyc()
        {
            var existing = await _context.KycDocuments.FirstOrDefaultAsync(k => k.UserId == OwnerId);
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

            var ownerId  = OwnerId;
            var existing = await _context.KycDocuments.FirstOrDefaultAsync(k => k.UserId == ownerId);

            var frontPath = await _fileSvc.UploadAsync(vm.NidFrontFile, "kyc", ownerId);
            var backPath  = await _fileSvc.UploadAsync(vm.NidBackFile,  "kyc", ownerId);
            var facePath  = await _fileSvc.UploadAsync(vm.FacePhotoFile, "kyc", ownerId);

            if (existing != null)
            {
                existing.NationalIdNumber = vm.NationalIdNumber;
                existing.Status           = KycStatus.Pending;
                existing.SubmittedAt      = DateTime.Now;
                existing.ReviewNote       = null;
                if (frontPath != null) existing.NidFrontPath = frontPath;
                if (backPath  != null) existing.NidBackPath  = backPath;
                if (facePath  != null) existing.FacePhotoPath = facePath;
                _context.KycDocuments.Update(existing);
            }
            else
            {
                _context.KycDocuments.Add(new KycDocument
                {
                    UserId           = ownerId,
                    NationalIdNumber = vm.NationalIdNumber,
                    NidFrontPath     = frontPath,
                    NidBackPath      = backPath,
                    FacePhotoPath    = facePath,
                    Status           = KycStatus.Pending,
                    SubmittedAt      = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "KYC submitted for review.";
            return RedirectToAction(nameof(Dashboard));
        }

        private static string BuildRules(RoomCreateViewModel vm)
        {
            var r = new List<string>();
            if (vm.NoSmoking) r.Add("No Smoking");
            if (vm.NoPets)    r.Add("No Pets");
            if (vm.GenderRule != "Any") r.Add($"{vm.GenderRule} Only");
            return string.Join("|", r);
        }
    }
}
