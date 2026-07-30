using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using BachelorRoomFinding.Services;
using BachelorRoomFinding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Controllers
{
    public class AccountController : Controller
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Role> _roleRepo;
        private readonly IRepository<LoginHistory> _loginHistRepo;
        private readonly EmailService _emailSvc;
        private readonly AppDbContext _context;

        public AccountController(
            IRepository<User> userRepo,
            IRepository<Role> roleRepo,
            IRepository<LoginHistory> loginHistRepo,
            EmailService emailSvc,
            AppDbContext context)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _loginHistRepo = loginHistRepo;
            _emailSvc = emailSvc;
            _context = context;
        }

        // ── Login ────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToRoleDashboard();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers["User-Agent"].ToString();

            if (!ModelState.IsValid) return View(model);

            var users = await _userRepo.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Email == model.Email);
            bool success = user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            // Log attempt
            await _loginHistRepo.AddAsync(new LoginHistory
            {
                UserId    = user?.UserId,     // null for failed logins - no FK violation
                LoginAt   = DateTime.Now,
                IpAddress = ip,
                UserAgent = ua,
                IsSuccess = success
            });

            if (!success)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (user!.AccountStatus == AccountStatus.Suspended)
            {
                ModelState.AddModelError("", "Your account has been suspended. Contact support.");
                return View(model);
            }

            // Set session
            HttpContext.Session.SetInt32("UserId",       user.UserId);
            HttpContext.Session.SetString("UserName",    user.UserName);
            HttpContext.Session.SetString("Role",        user.Role.RoleName);
            HttpContext.Session.SetString("IsApproved",  user.IsApprovedByAdmin.ToString());
            HttpContext.Session.SetString("IsVerified",  user.IsVerified.ToString());

            // Update last login
            user.LastLogin = DateTime.Now;
            await _userRepo.UpdateAsync(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToRoleDashboard();
        }

        // ── Register ─────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existing = (await _userRepo.GetAllAsync()).Any(u => u.Email == model.Email);
            if (existing)
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            var roles = await _roleRepo.GetAllAsync();
            var role  = roles.FirstOrDefault(r => r.RoleName == model.Role);
            if (role == null)
            {
                ModelState.AddModelError("", "Invalid role selected.");
                return View(model);
            }

            var token = Guid.NewGuid().ToString("N");
            var newUser = new User
            {
                UserName              = model.UserName,
                Email                 = model.Email,
                PasswordHash          = BCrypt.Net.BCrypt.HashPassword(model.Password),
                PhoneNumber           = model.PhoneNumber,
                Address               = model.Address,
                RoleId                = role.Id,
                IsApprovedByAdmin     = role.RoleName == "User", // users auto-approved; owners need admin
                IsEmailVerified       = false,
                AccountStatus         = AccountStatus.Active,
                EmailVerificationToken = token,
                CreatedAt             = DateTime.Now
            };

            await _userRepo.AddAsync(newUser);
            await _emailSvc.SendVerificationAsync(newUser.Email, newUser.UserName, token);

            TempData["Success"] = "Registration successful! Please check your email to verify your account.";
            return RedirectToAction("Login");
        }

        // ── Email Verify ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            var user = (await _userRepo.GetAllAsync()).FirstOrDefault(u => u.EmailVerificationToken == token);
            if (user == null) { TempData["Error"] = "Invalid or expired token."; return RedirectToAction("Login"); }

            user.IsEmailVerified       = true;
            user.EmailVerificationToken = null;
            await _userRepo.UpdateAsync(user);

            TempData["Success"] = "Email verified! You can now log in.";
            return RedirectToAction("Login");
        }

        // ── Forgot Password ───────────────────────────────────────────
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = (await _userRepo.GetAllAsync()).FirstOrDefault(u => u.Email == model.Email);
            if (user != null)
            {
                user.PasswordResetToken  = Guid.NewGuid().ToString("N");
                user.PasswordResetExpiry = DateTime.Now.AddHours(2);
                await _userRepo.UpdateAsync(user);
                await _emailSvc.SendPasswordResetAsync(user.Email, user.UserName, user.PasswordResetToken);
            }

            TempData["Success"] = "If that email exists, a reset link has been sent.";
            return RedirectToAction("Login");
        }

        // ── Reset Password ────────────────────────────────────────────
        [HttpGet]
        public IActionResult ResetPassword(string token) => View(new ResetPasswordViewModel { Token = token });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = (await _userRepo.GetAllAsync())
                .FirstOrDefault(u => u.PasswordResetToken == model.Token && u.PasswordResetExpiry > DateTime.Now);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid or expired reset token.");
                return View(model);
            }

            user.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.PasswordResetToken  = null;
            user.PasswordResetExpiry = null;
            await _userRepo.UpdateAsync(user);

            TempData["Success"] = "Password reset successfully. Please log in.";
            return RedirectToAction("Login");
        }

        // ── Access Denied ─────────────────────────────────────────────
        public IActionResult AccessDenied() => View();

        // ── Pending Approval ─────────────────────────────────────────
        public IActionResult PendingApproval() => View();

        // ── Logout ────────────────────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ── Helper ────────────────────────────────────────────────────
        private IActionResult RedirectToRoleDashboard()
        {
            var role = HttpContext.Session.GetString("Role");
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Owner" => RedirectToAction("Dashboard", "Owner"),
                _       => RedirectToAction("Dashboard", "UserDashboard")
            };
        }
    }
}
