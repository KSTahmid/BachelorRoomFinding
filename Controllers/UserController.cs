using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Filters;
using BachelorRoomFinding.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BachelorRoomFinding.Controllers
{
    [RequireRole("Admin")]
    public class UserController : Controller
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Role> _roleRepo;

        public UserController(IRepository<User> userRepo, IRepository<Role> roleRepo)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            var result = await _userRepo.GetPagedAsync(page, pageSize, search);
            ViewBag.Search   = search;
            ViewBag.PageSize = pageSize;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _roleRepo.GetAllAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, string plainPassword, int RoleId)
        {
            ModelState.Remove("Role");
            ModelState.Remove("PasswordHash");
            ModelState.Remove("LoginHistories");
            ModelState.Remove("Notifications");

            if (string.IsNullOrWhiteSpace(plainPassword))
                ModelState.AddModelError("PasswordHash", "Password is required.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _roleRepo.GetAllAsync();
                return View(user);
            }

            try
            {
                user.RoleId       = RoleId;
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                user.CreatedAt    = DateTime.Now;
                user.AccountStatus = AccountStatus.Active;
                await _userRepo.AddAsync(user);
                TempData["Success"] = "User created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                ViewBag.Roles = await _roleRepo.GetAllAsync();
                return View(user);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.Roles = await _roleRepo.GetAllAsync();
            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User user, string? plainPassword, int RoleId)
        {
            ModelState.Remove("Role");
            ModelState.Remove("PasswordHash");
            ModelState.Remove("LoginHistories");
            ModelState.Remove("Notifications");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _roleRepo.GetAllAsync();
                return View(user);
            }

            try
            {
                user.RoleId = RoleId;
                if (!string.IsNullOrWhiteSpace(plainPassword))
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                else
                {
                    var existing = await _userRepo.GetByIdAsync(user.UserId);
                    user.PasswordHash = existing?.PasswordHash ?? "";
                }

                await _userRepo.UpdateAsync(user);
                TempData["Success"] = "User updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                ViewBag.Roles = await _roleRepo.GetAllAsync();
                return View(user);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _userRepo.DeleteAsync(id);
            TempData["Success"] = "User deleted.";
            return RedirectToAction(nameof(Index));
        }

        // Quick approve/suspend toggle
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            user.IsApprovedByAdmin = !user.IsApprovedByAdmin;
            await _userRepo.UpdateAsync(user);
            TempData["Success"] = $"User {(user.IsApprovedByAdmin ? "approved" : "unapproved")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int userId, int roleId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound();

            var role = await _roleRepo.GetByIdAsync(roleId);
            if (role == null) return BadRequest("Invalid role selected.");

            user.RoleId = roleId;
            await _userRepo.UpdateAsync(user);
            TempData["Success"] = $"Role updated to {role.RoleName} for user {user.UserName}.";
            return RedirectToAction(nameof(Index));
        }
    }
}