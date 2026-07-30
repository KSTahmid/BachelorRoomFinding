using BachelorRoomFinding.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IOtpService _otpService;
        private readonly BachelorRoomFinding.Data.AppDbContext _context;

        public PaymentController(IPaymentService paymentService, IOtpService otpService, BachelorRoomFinding.Data.AppDbContext context)
        {
            _paymentService = paymentService;
            _otpService = otpService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Choose(int applicationId, decimal amount)
        {
            var application = await _context.RentalApplications
                .Include(a => a.Room)
                .ThenInclude(r => r.Owner)
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            if (application == null) return NotFound();

            ViewBag.ApplicationId = applicationId;
            ViewBag.Amount = amount;
            ViewBag.OwnerBkash = application.Room.Owner.BkashNumber ?? application.Room.Owner.PhoneNumber ?? "01XXXXXXXXX";
            ViewBag.OwnerNagad = application.Room.Owner.NagadNumber ?? application.Room.Owner.PhoneNumber ?? "01XXXXXXXXX";
            ViewBag.IsDemoNumber = application.Room.Owner.IsDemoNumber;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Initiate(int applicationId, decimal amount, string method, string senderWalletNumber)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var application = await _context.RentalApplications
                .Include(a => a.Room)
                .ThenInclude(r => r.Owner)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.ApplicantId == userId.Value);
            if (application == null) return NotFound();
            if (application.Room.Owner.IsDemoNumber && (method == "bKash" || method == "Nagad"))
            {
                TempData["Error"] = "এই owner এর payment number টি demo। বাস্তব পেমেন্ট করা যাবে না।";
                return RedirectToAction(nameof(Choose), new { applicationId, amount });
            }

            var payment = await _paymentService.InitializePaymentAsync(applicationId, userId.Value, method, amount, senderWalletNumber);

            if (method == "bKash" || method == "Nagad")
            {
                return RedirectToAction("MockGateway", new { paymentId = payment.Id, method = method, amount = amount });
            }

            // Fetch user phone from DB for OTP for other methods
            var user = await _context.Users.FindAsync(userId.Value);
            string userPhone = user?.PhoneNumber ?? "01700000000"; 
            await _otpService.GenerateAndSendOtpAsync(userPhone);

            return RedirectToAction("VerifyOtp", new { paymentId = payment.Id, phone = userPhone });
        }

        [HttpGet]
        public async Task<IActionResult> MockGateway(int paymentId, string method, decimal amount)
        {
            var payment = await _context.Payments
                .Include(p => p.Application)
                .ThenInclude(a => a.Room)
                .ThenInclude(r => r.Owner)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) return NotFound();

            ViewBag.PaymentId = paymentId;
            ViewBag.Method = method;
            ViewBag.Amount = amount;
            ViewBag.OwnerPhone = payment.RecipientWalletNumber
                ?? payment.Application?.Room?.Owner?.PhoneNumber
                ?? "01XXXXXXXXX";
            ViewBag.SenderPhone = payment.SenderWalletNumber;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MockGatewaySubmit(int paymentId, string method, string otp, string pin)
        {
            if (string.IsNullOrWhiteSpace(otp) || otp.Trim().Length < 4)
            {
                TempData["Error"] = "Enter the sandbox OTP to continue.";
                return RedirectToAction(nameof(MockGateway), new { paymentId, method, amount = 0m });
            }
            if (string.IsNullOrWhiteSpace(pin) || pin.Trim().Length < 4)
            {
                TempData["Error"] = "Enter a valid sandbox PIN.";
                return RedirectToAction(nameof(MockGateway), new { paymentId, method, amount = 0m });
            }

            string transactionId = method.Substring(0,1).ToUpper() + "X" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            string status = "success";

            if (method == "bKash")
            {
                return RedirectToAction("bKashCallback", new { paymentId = paymentId, transactionId = transactionId, status = status });
            }
            else
            {
                return RedirectToAction("NagadCallback", new { paymentId = paymentId, transactionId = transactionId, status = status });
            }
        }

        [HttpGet]
        public async Task<IActionResult> bKashCallback(int paymentId, string transactionId, string status)
        {
            if (status == "success")
            {
                var success = await _paymentService.CompletePaymentAsync(paymentId, transactionId, null);
                if (success)
                {
                    TempData["Success"] = "bKash Payment successful and booking confirmed!";
                    return RedirectToAction("Dashboard", "UserDashboard");
                }
            }
            TempData["Error"] = "bKash Payment failed or could not be verified.";
            return RedirectToAction("Dashboard", "UserDashboard");
        }

        [HttpGet]
        public async Task<IActionResult> NagadCallback(int paymentId, string transactionId, string status)
        {
            if (status == "success")
            {
                var success = await _paymentService.CompletePaymentAsync(paymentId, transactionId, null);
                if (success)
                {
                    TempData["Success"] = "Nagad Payment successful and booking confirmed!";
                    return RedirectToAction("Dashboard", "UserDashboard");
                }
            }
            TempData["Error"] = "Nagad Payment failed or could not be verified.";
            return RedirectToAction("Dashboard", "UserDashboard");
        }

        [HttpGet]
        public async Task<IActionResult> VerifyOtp(int paymentId, string phone)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return NotFound();

            ViewBag.PaymentId = paymentId;
            ViewBag.Phone = phone;
            ViewBag.Method = payment.Method;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtpConfirm(int paymentId, string transactionId, string? otpCode, string phone)
        {
            var success = await _paymentService.CompletePaymentAsync(paymentId, transactionId, otpCode);
            if (success)
            {
                TempData["Success"] = "Payment successful and booking confirmed!";
                return RedirectToAction("Dashboard", "UserDashboard");
            }

            TempData["Error"] = "Invalid transaction ID or OTP. Please try again.";
            return RedirectToAction("VerifyOtp", new { paymentId = paymentId, phone = phone });
        }
    }
}
