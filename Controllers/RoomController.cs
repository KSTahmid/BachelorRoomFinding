using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Filters;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Services;
using BachelorRoomFinding.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BachelorRoomFinding.Controllers
{
    [RequireRole("Admin")]
    public class RoomController : Controller
    {
        private readonly IRoomRepository _roomRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Role> _roleRepo;
        private readonly FileUploadService _fileSvc;

        public RoomController(IRoomRepository roomRepo, IRepository<User> userRepo,
            IRepository<Role> roleRepo, FileUploadService fileSvc)
        {
            _roomRepo = roomRepo;
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _fileSvc  = fileSvc;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            var result = await _roomRepo.GetPagedAsync(page, pageSize, search);
            ViewBag.Search   = search;
            ViewBag.PageSize = pageSize;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateOwners();
            return View(new RoomCreateViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomCreateViewModel vm)
        {
            ModelState.Remove("MediaFiles");
            ModelState.Remove("Owner");
            if (!ModelState.IsValid) { await PopulateOwners(); return View(vm); }

            try
            {
                var room = MapToRoom(vm);
                room.PostedDate = DateTime.Now;
                await _roomRepo.AddAsync(room);

                await SaveFacilitiesAndPhotos(room.Id, vm, room.OwnerId);
                TempData["Success"] = "Room created!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateOwners();
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _roomRepo.GetByIdAsync(id);
            if (room == null) return NotFound();
            await PopulateOwners();
            var vm = MapToViewModel(room);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoomCreateViewModel vm)
        {
            if (!ModelState.IsValid) { await PopulateOwners(); return View(vm); }

            try
            {
                var room = await _roomRepo.GetByIdAsync(vm.Id);
                if (room == null) return NotFound();

                var ctx = HttpContext.RequestServices.GetRequiredService<Data.AppDbContext>();

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
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateOwners();
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _roomRepo.GetByIdAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _roomRepo.DeleteAsync(id);
            TempData["Success"] = "Room deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var room = await _roomRepo.GetByIdAsync(id);
            if (room == null) return NotFound();
            room.Status = RoomStatus.Active;
            await _roomRepo.UpdateAsync(room);
            TempData["Success"] = "Room approved and listed.";
            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ───────────────────────────────────────────────────
        private async Task PopulateOwners()
        {
            var roles  = await _roleRepo.GetAllAsync();
            var ownerRole = roles.FirstOrDefault(r => r.RoleName == "Owner");
            var owners = ownerRole != null
                ? (await _userRepo.GetAllAsync()).Where(u => u.RoleId == ownerRole.Id)
                : Enumerable.Empty<User>();
            ViewBag.Owners = owners;
        }

        private static Room MapToRoom(RoomCreateViewModel vm) => new()
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
            OwnerId         = vm.OwnerId,
            Status          = vm.Status,
            Rules           = BuildRules(vm)
        };

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
            room.OwnerId         = vm.OwnerId;
            room.Status          = vm.Status;
            room.Rules           = BuildRules(vm);
        }

        private static string BuildRules(RoomCreateViewModel vm)
        {
            var rules = new List<string>();
            if (vm.SmokingAllowed) rules.Add("Smoking Allowed"); else rules.Add("No Smoking");
            if (vm.GuestAllowed) rules.Add("Guest Allowed"); else rules.Add("No Guests");
            if (vm.BachelorOnly) rules.Add("Bachelor Only");
            if (vm.FamilyRestricted) rules.Add("Family Restricted");
            if (!string.IsNullOrWhiteSpace(vm.CurfewTiming)) rules.Add($"Curfew: {vm.CurfewTiming}");
            return string.Join("|", rules);
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
            SelectedFacilities = room.Facilities.Select(f => f.FacilityName).ToList(),
            SmokingAllowed  = room.Rules?.Contains("Smoking Allowed") ?? false,
            GuestAllowed    = room.Rules?.Contains("Guest Allowed") ?? false,
            BachelorOnly    = room.Rules?.Contains("Bachelor Only") ?? false,
            FamilyRestricted= room.Rules?.Contains("Family Restricted") ?? false,
            CurfewTiming    = room.Rules?.Split('|').FirstOrDefault(r => r.StartsWith("Curfew: "))?.Replace("Curfew: ", "") ?? ""
        };

        private async Task SaveFacilitiesAndPhotos(int roomId, RoomCreateViewModel vm, int ownerId)
        {
            var ctx = HttpContext.RequestServices.GetRequiredService<Data.AppDbContext>();
            foreach (var f in vm.SelectedFacilities)
                ctx.RoomFacilities.Add(new RoomFacility { RoomId = roomId, FacilityName = f });

            if (vm.MediaFiles?.Any() == true)
            {
                bool first = true;
                foreach (var file in vm.MediaFiles)
                {
                    var path = await _fileSvc.UploadAsync(file, "rooms", ownerId);
                    if (path != null)
                    {
                        bool isVideo = file.ContentType.StartsWith("video/");
                        ctx.RoomPhotos.Add(new RoomPhoto { RoomId = roomId, PhotoPath = path, IsPrimary = first, IsVideo = isVideo });
                        first = false;
                    }
                }
            }
            await ctx.SaveChangesAsync();
        }
    }
}
