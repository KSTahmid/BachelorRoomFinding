using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BachelorRoomFinding.Controllers
{
    public class UserController : Controller
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Role> _roleRepo;

        public UserController(IRepository<User> userRepo, IRepository<Role> roleRepo)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "")
        {
            var result = await _userRepo.GetPagedAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _roleRepo.GetAllAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (ModelState.IsValid)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                await _userRepo.AddAsync(user);
                TempData["Success"] = "User created successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Roles = await _roleRepo.GetAllAsync();
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.Roles = await _roleRepo.GetAllAsync();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User user)
        {
            if (ModelState.IsValid)
            {
                await _userRepo.UpdateAsync(user);
                TempData["Success"] = "User updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Roles = await _roleRepo.GetAllAsync();
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _userRepo.DeleteAsync(id);
            TempData["Success"] = "User deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
