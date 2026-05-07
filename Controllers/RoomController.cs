using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BachelorRoomFinding.Controllers
{
    public class RoomController : Controller
    {
        private readonly IRepository<Room> _roomRepo;
        private readonly IRepository<User> _userRepo;

        public RoomController(IRepository<Room> roomRepo, IRepository<User> userRepo)
        {
            _roomRepo = roomRepo;
            _userRepo = userRepo;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "")
        {
            var result = await _roomRepo.GetPagedAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Owners = await _userRepo.GetAllAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            if (ModelState.IsValid)
            {
                room.PostedDate = DateTime.Now;
                await _roomRepo.AddAsync(room);
                TempData["Success"] = "Room posted successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Owners = await _userRepo.GetAllAsync();
            return View(room);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _roomRepo.GetByIdAsync(id);
            if (room == null) return NotFound();
            ViewBag.Owners = await _userRepo.GetAllAsync();
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Room room)
        {
            if (ModelState.IsValid)
            {
                await _roomRepo.UpdateAsync(room);
                TempData["Success"] = "Room updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Owners = await _userRepo.GetAllAsync();
            return View(room);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _roomRepo.GetByIdAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _roomRepo.DeleteAsync(id);
            TempData["Success"] = "Room deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
