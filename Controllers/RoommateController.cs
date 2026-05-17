using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Filters;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Controllers
{
    [RequireLogin]
    public class RoommateController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRepository<RoommateAd> _adRepo;
        private readonly NotificationService _notifSvc;

        public RoommateController(AppDbContext context, IRepository<RoommateAd> adRepo, NotificationService notifSvc)
        {
            _context = context;
            _adRepo = adRepo;
            _notifSvc = notifSvc;
        }

        private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

        [AllowAnonymousAccess]
        public async Task<IActionResult> Index()
        {
            var ads = await _context.RoommateAds
                .Include(a => a.User)
                .Where(a => a.Status == RoommateAdStatus.Active)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId.HasValue)
            {
                var myPrefs = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == currentUserId.Value);
                ViewBag.MyPrefs = myPrefs;
                
                // Pre-calculate matching scores
                var otherPrefs = await _context.UserPreferences.Where(p => p.UserId != currentUserId.Value).ToListAsync();
                ViewBag.OtherPrefs = otherPrefs;
            }

            return View(ads);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var prefs = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == UserId);
            ViewBag.Prefs = prefs ?? new UserPreference();
            return View(new RoommateAd());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoommateAd ad, UserPreference prefs)
        {
            ModelState.Remove("User");
            if (!ModelState.IsValid) return View(ad);

            ad.UserId = UserId;
            ad.CreatedAt = DateTime.Now;
            _context.RoommateAds.Add(ad);

            // Update or Create Preferences
            var existingPrefs = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == UserId);
            if (existingPrefs != null)
            {
                existingPrefs.Smoking = prefs.Smoking;
                existingPrefs.SleepSchedule = prefs.SleepSchedule;
                existingPrefs.Cleanliness = prefs.Cleanliness;
                existingPrefs.FoodHabit = prefs.FoodHabit;
                existingPrefs.PrayerHabit = prefs.PrayerHabit;
                existingPrefs.GuestPolicy = prefs.GuestPolicy;
                existingPrefs.PetFriendly = prefs.PetFriendly;
                _context.UserPreferences.Update(existingPrefs);
            }
            else
            {
                prefs.UserId = UserId;
                _context.UserPreferences.Add(prefs);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Roommate ad posted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Connect(int adId, string message)
        {
            var ad = await _context.RoommateAds.FindAsync(adId);
            if (ad == null) return NotFound();

            if (ad.UserId == UserId) return BadRequest("You cannot connect to your own ad.");

            var request = new RoommateConnectionRequest
            {
                SenderUserId = UserId,
                RoommateAdId = adId,
                Message = message,
                Status = ConnectionRequestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.RoommateConnectionRequests.Add(request);
            await _context.SaveChangesAsync();

            await _notifSvc.CreateAsync(ad.UserId, "New Roommate Connection Request", 
                "Someone is interested in your roommate ad!", NotificationType.NewMessage);

            TempData["Success"] = "Connection request sent!";
            return RedirectToAction(nameof(Index));
        }
    }
}
