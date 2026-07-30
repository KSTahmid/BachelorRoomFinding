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
    [RequireApproval]
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
            ModelState.Remove("MediaFiles");
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
                    MonthlyRent     = vm.MonthlyRent,
                    SeatRent        = vm.SeatRent,
                    ElectricityBill = vm.ElectricityBill,
                    WiFiBill        = vm.WiFiBill,
                    GasBill         = vm.GasBill,
                    WaterBill       = vm.WaterBill,
                    ServiceCharge   = vm.ServiceCharge,
                    MealCost        = vm.MealCost,
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

                // Media (Photos & Videos)
                if (vm.MediaFiles?.Any() == true)
                {
                    bool first = true;
                    foreach (var file in vm.MediaFiles)
                    {
                        var path = await _fileSvc.UploadAsync(file, "rooms", ownerId);
                        if (path != null)
                        {
                            bool isVideo = file.ContentType.StartsWith("video/");
                            _context.RoomPhotos.Add(new RoomPhoto { RoomId = room.Id, PhotoPath = path, IsPrimary = first, IsVideo = isVideo });
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

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _roomRepo.GetByIdAsync(id);
            if (room == null || room.OwnerId != OwnerId) return NotFound();
            var vm = MapToViewModel(room);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoomCreateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            try
            {
                var room = await _roomRepo.GetByIdAsync(vm.Id);
                if (room == null || room.OwnerId != OwnerId) return NotFound();

                var ctx = HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                // Prevent owner from maliciously changing owner or status
                vm.OwnerId = OwnerId;
                vm.Status = room.Status;

                UpdateRoomFromVm(room, vm);
                
                // Update facilities
                var existingFacilities = ctx.RoomFacilities.Where(f => f.RoomId == room.Id);
                ctx.RoomFacilities.RemoveRange(existingFacilities);
                foreach (var f in vm.SelectedFacilities)
                    ctx.RoomFacilities.Add(new RoomFacility { RoomId = room.Id, FacilityName = f });

                // Update photos if provided
                if (vm.MediaFiles?.Any() == true)
                {
                    var existingPhotos = ctx.RoomPhotos.Where(p => p.RoomId == room.Id);
                    ctx.RoomPhotos.RemoveRange(existingPhotos);
                    
                    bool first = true;
                    foreach (var file in vm.MediaFiles)
                    {
                        var path = await _fileSvc.UploadAsync(file, "rooms", room.OwnerId);
                        if (path != null)
                        {
                            bool isVideo = file.ContentType.StartsWith("video/");
                            ctx.RoomPhotos.Add(new RoomPhoto { RoomId = room.Id, PhotoPath = path, IsPrimary = first, IsVideo = isVideo });
                            first = false;
                        }
                    }
                }
                
                await ctx.SaveChangesAsync();
                await _roomRepo.UpdateAsync(room);

                TempData["Success"] = "Room updated!";
                return RedirectToAction(nameof(MyRooms));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
        }

        private static void UpdateRoomFromVm(Room room, RoomCreateViewModel vm)
        {
            room.Title           = vm.Title;
            room.Description     = vm.Description;
            room.Address         = vm.Address;
            room.District        = vm.District;
            room.Thana           = vm.Thana;
            room.MonthlyRent     = vm.MonthlyRent;
            room.SeatRent        = vm.SeatRent;
            room.ElectricityBill = vm.ElectricityBill;
            room.WiFiBill        = vm.WiFiBill;
            room.GasBill         = vm.GasBill;
            room.WaterBill       = vm.WaterBill;
            room.ServiceCharge   = vm.ServiceCharge;
            room.MealCost        = vm.MealCost;
            room.SecurityDeposit = vm.SecurityDeposit;
            room.Advance         = vm.Advance;
            room.BedroomCount    = vm.BedroomCount;
            room.RoomType        = vm.RoomType;
            room.AvailableFrom   = vm.AvailableFrom;
            room.Rules           = BuildRules(vm);
        }

        private static RoomCreateViewModel MapToViewModel(Room room) => new()
        {
            Id              = room.Id,
            Title           = room.Title,
            Description     = room.Description,
            Address         = room.Address,
            District        = room.District,
            Thana           = room.Thana,
            MonthlyRent     = room.MonthlyRent,
            SeatRent        = room.SeatRent,
            ElectricityBill = room.ElectricityBill,
            WiFiBill        = room.WiFiBill,
            GasBill         = room.GasBill,
            WaterBill       = room.WaterBill,
            ServiceCharge   = room.ServiceCharge,
            MealCost        = room.MealCost,
            SecurityDeposit = room.SecurityDeposit,
            Advance         = room.Advance,
            BedroomCount    = room.BedroomCount,
            RoomType        = room.RoomType,
            AvailableFrom   = room.AvailableFrom,
            OwnerId         = room.OwnerId,
            Status          = room.Status,
            SelectedFacilities = room.Facilities?.Select(f => f.FacilityName).ToList() ?? new List<string>(),
            SmokingAllowed  = room.Rules?.Contains("Smoking Allowed") ?? false,
            GuestAllowed    = room.Rules?.Contains("Guest Allowed") ?? false,
            BachelorOnly    = room.Rules?.Contains("Bachelor Only") ?? false,
            FamilyRestricted= room.Rules?.Contains("Family Restricted") ?? false,
            CurfewTiming    = room.Rules?.Split('|').FirstOrDefault(r => r.StartsWith("Curfew: "))?.Replace("Curfew: ", "") ?? ""
        };

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

            app.Payment.Status = "Completed";
            app.Payment.ConfirmedByUserId = OwnerId;
            
            // Mark room as rented? Or just application as settled?
            // Usually we mark the room as occupied once payment is confirmed.
            app.Room.IsAvailable = false;
            app.Room.Status = RoomStatus.Rented;

            // Auto-join tenant to room's MessBoard community
            var messGroup = await _context.MessGroups.FirstOrDefaultAsync(g => g.RoomId == app.RoomId);
            if (messGroup != null)
            {
                var alreadyMember = await _context.MessMembers.AnyAsync(m => m.MessGroupId == messGroup.Id && m.UserId == app.ApplicantId);
                if (!alreadyMember)
                {
                    _context.MessMembers.Add(new MessMember
                    {
                        MessGroupId = messGroup.Id,
                        UserId = app.ApplicantId,
                        Role = MessRole.Tenant,
                        IsManager = false,
                        JoinedAt = DateTime.Now
                    });
                }
            }

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

        // ── Roommate Vacancies ───────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> PostRoommateAd(int roomId)
        {
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null || room.OwnerId != OwnerId) return Forbid();

            var ad = new RoommateAd
            {
                RoomId = roomId,
                PreferredAreas = $"{room.Thana}, {room.District}",
                MaxRentPerPerson = room.SeatRent > 0 ? room.SeatRent : room.MonthlyRent,
                AdvancePaymentAmount = room.Advance,
                Description = $"Looking for a roommate for our {room.RoomType} at {room.Address}."
            };
            return View(ad);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PostRoommateAd(RoommateAd ad)
        {
            var room = await _roomRepo.GetByIdAsync(ad.RoomId.GetValueOrDefault());
            if (room == null || room.OwnerId != OwnerId) return Forbid();

            ModelState.Remove("User");
            ModelState.Remove("Room");
            if (!ModelState.IsValid) return View(ad);

            ad.UserId = OwnerId;
            ad.CreatedAt = DateTime.Now;
            ad.Status = RoommateAdStatus.Active;

            _context.RoommateAds.Add(ad);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Roommate vacancy posted!";
            return RedirectToAction(nameof(MyRooms));
        }

        [HttpGet]
        public async Task<IActionResult> RoommateRequests(int adId)
        {
            var ad = await _context.RoommateAds
                .Include(a => a.Room)
                .Include(a => a.ConnectionRequests)
                .ThenInclude(r => r.Sender)
                .FirstOrDefaultAsync(a => a.Id == adId && a.UserId == OwnerId);
            
            if (ad == null) return NotFound();
            return View(ad);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRoommate(int requestId)
        {
            var req = await _context.RoommateConnectionRequests
                .Include(r => r.RoommateAd)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (req == null || req.RoommateAd.UserId != OwnerId) return Forbid();

            req.Status = ConnectionRequestStatus.Accepted;
            req.RespondedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await _notifSvc.CreateAsync(req.SenderUserId, "Roommate Request Accepted", 
                "Your request was accepted. You can now message this profile.", NotificationType.ApplicationStatus);
            
            TempData["Success"] = "Roommate accepted. Waiting for advance payment.";
            return RedirectToAction(nameof(RoommateRequests), new { adId = req.RoommateAdId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRoommatePayment(int requestId)
        {
            var req = await _context.RoommateConnectionRequests
                .Include(r => r.RoommateAd)
                .ThenInclude(a => a.Room)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (req == null || req.RoommateAd.UserId != OwnerId) return Forbid();

            req.PaymentStatus = "Completed";
            req.RoommateAd.Status = RoommateAdStatus.Closed;
            
            if (req.RoommateAd.Room != null)
            {
                req.RoommateAd.Room.IsAvailable = false;
                req.RoommateAd.Room.Status = RoomStatus.Rented;
            }

            await _context.SaveChangesAsync();

            await _notifSvc.CreateAsync(req.SenderUserId, "Payment Confirmed", 
                "Your payment is confirmed. Welcome to your new room!", NotificationType.PaymentConfirmed);
            
            TempData["Success"] = "Payment confirmed and roommate added!";
            return RedirectToAction(nameof(MyRooms));
        }

        private static string BuildRules(RoomCreateViewModel vm)
        {
            var r = new List<string>();
            if (vm.SmokingAllowed) r.Add("Smoking Allowed"); else r.Add("No Smoking");
            if (vm.GuestAllowed) r.Add("Guest Allowed"); else r.Add("No Guests");
            if (vm.BachelorOnly) r.Add("Bachelor Only");
            if (vm.FamilyRestricted) r.Add("Family Restricted");
            if (!string.IsNullOrWhiteSpace(vm.CurfewTiming)) r.Add($"Curfew: {vm.CurfewTiming}");
            return string.Join("|", r);
        }
    }
}
