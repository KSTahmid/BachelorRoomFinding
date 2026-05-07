using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BachelorRoomFinding.Controllers
{
    public class RoleController : Controller
    {
        private readonly IRepository<Role> _roleRepo;

        public RoleController(IRepository<Role> roleRepo) => _roleRepo = roleRepo;

        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "")
        {
            var result = await _roleRepo.GetPagedAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;
            return View(result);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            if (ModelState.IsValid)
            {
                await _roleRepo.AddAsync(role);
                TempData["Success"] = "Role created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _roleRepo.GetByIdAsync(id);
            if (role == null) return NotFound();
            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Role role)
        {
            if (ModelState.IsValid)
            {
                await _roleRepo.UpdateAsync(role);
                TempData["Success"] = "Role updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _roleRepo.GetByIdAsync(id);
            if (role == null) return NotFound();
            return View(role);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _roleRepo.DeleteAsync(id);
            TempData["Success"] = "Role deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
