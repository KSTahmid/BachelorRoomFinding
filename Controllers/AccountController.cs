using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BachelorRoomFinding.Controllers
{
    public class AccountController : Controller
    {
        private readonly IRepository<User> _userRepo;

        public AccountController(IRepository<User> userRepo) => _userRepo = userRepo;

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var users = await _userRepo.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Email == email);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("Role", user.Role.RoleName);
                user.LastLogin = DateTime.Now;
                await _userRepo.UpdateAsync(user);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid email or password!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
