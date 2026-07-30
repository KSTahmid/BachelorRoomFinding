using BachelorRoomFinding.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Controllers
{
    /// <summary>
    /// Handles all payment-related operations for the Bachelor Room Finding platform.
    /// Supports bKash and Nagad mobile banking payment methods.
    /// Payment flow: Choose → Initiate → MockGateway → Callback → Complete
    /// </summary>
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IOtpService _otpService;
        private readonly BachelorRoomFinding.Data.AppDbContext _context;

        /// <summary>
        /// Initializes PaymentController with required services via dependency injection.
        /// </summary>
        public PaymentController(IPaymentService paymentService, IOtpService otpService, BachelorRoomFinding.Data.AppDbContext context)
        {
            _paymentService = paymentService;
            _otpService = otpService;
            _context = context;
        }

        /// <summary>
        /// Displays the payment method selection screen for a given rental application.
        /// Fetches the owner's bKash and Nagad wallet numbers to show as recipient.
        /// </summary>
        /// <param name="applicationId">The ID of the approved rental application to pay for.</param>
        /// <param name="amount">The advance/deposit amount to be paid in BDT.</param>
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

            // Prefer dedicated wallet numbers; fall back to general phone number
            ViewBag.OwnerBkash = application.Room.Owner.BkashNumber ?? application.Room.Owner.PhoneNumber ?? "01XXXXXXXXX";
            ViewBag.OwnerNagad = application.Room.Owner.NagadNumber ?? application.Room.Owner.PhoneNumber ?? "01XXXXXXXXX";
            ViewBag.IsDemoNumber = application.Room.Owner.IsDemoNumber;
            return View();
        }

        /// <summary>
        /// Initiates a payment after the user selects a payment method.
        /// For bKash/Nagad, redirects to the simulated gateway (MockGateway).
        /// For other methods (Bank Transfer etc.), generates and sends an OTP.
        /// </summary>
        /// <param name="applicationId">The rental application being paid for.</param>
        /// <param name="amount">Amount in BDT.</param>
        /// <param name="method">Payment method: "bKash", "Nagad", or "BankTransfer".</param>
        /// <param name="senderWalletNumber">The tenant's wallet/account number.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Initiate(int applicationId, decimal amount, string method, string senderWalletNumber)
        {
            // Ensure user is logged in
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            // Verify the application belongs to the logged-in user
            var application = await _context.RentalApplications
                .Include(a => a.Room)
                .ThenInclude(r => r.Owner)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.ApplicantId == userId.Value);
            if (application == null) return NotFound();

            // Initialize or update the payment record (upsert to avoid duplicate key violations)
            var payment = await _paymentService.InitializePaymentAsync(applicationId, userId.Value, method, amount, senderWalletNumber);

            if (method == "bKash" || method == "Nagad")
            {
                // Route to the simulated bKash/Nagad checkout gateway
                return RedirectToAction("MockGateway", new { paymentId = payment.Id, method = method, amount = amount });
            }

            // Fetch user phone from DB for OTP for non-mobile-banking methods
            var user = await _context.Users.FindAsync(userId.Value);
            string userPhone = user?.PhoneNumber ?? "01700000000";
            await _otpService.GenerateAndSendOtpAsync(userPhone);

            return RedirectToAction("VerifyOtp", new { paymentId = payment.Id, phone = userPhone });
        }

        /// <summary>
        /// Renders the simulated bKash/Nagad payment gateway UI.
        /// Mimics the real bKash and Nagad checkout experience with step-by-step flow:
        /// wallet number → verification code → PIN → confirm.
        /// </summary>
        /// <param name="paymentId">The payment record ID.</param>
        /// <param name="method">Payment method to style the gateway accordingly.</param>
        /// <param name="amount">Amount displayed on the gateway screen.</param>
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

            // Show owner's wallet number as the merchant/recipient number
            ViewBag.OwnerPhone = payment.RecipientWalletNumber
                ?? payment.Application?.Room?.Owner?.PhoneNumber
                ?? "01XXXXXXXXX";
            ViewBag.SenderPhone = payment.SenderWalletNumber;
            return View();
        }

        /// <summary>
        /// Processes the gateway form submission (simulated OTP + PIN validation).
        /// Generates a unique transaction ID and routes to the appropriate payment callback.
        /// </summary>
        /// <param name="paymentId">The payment record ID.</param>
        /// <param name="method">The selected payment method.</param>
        /// <param name="otp">Verification code entered by the user.</param>
        /// <param name="pin">PIN entered by the user for authorization.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MockGatewaySubmit(int paymentId, string method, string otp, string pin)
        {
            // Validate OTP input (min 4 chars)
            if (string.IsNullOrWhiteSpace(otp) || otp.Trim().Length < 4)
            {
                TempData["Error"] = "Enter the OTP to continue.";
                return RedirectToAction(nameof(MockGateway), new { paymentId, method, amount = 0m });
            }
            // Validate PIN input (min 4 chars)
            if (string.IsNullOrWhiteSpace(pin) || pin.Trim().Length < 4)
            {
                TempData["Error"] = "Enter a valid PIN.";
                return RedirectToAction(nameof(MockGateway), new { paymentId, method, amount = 0m });
            }

            // Generate a realistic-looking transaction ID (e.g. BX3A1F2C for bKash)
            string transactionId = method.Substring(0, 1).ToUpper() + "X" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            string status = "success";

            // Route to method-specific callback handler
            if (method == "bKash")
            {
                return RedirectToAction("bKashCallback", new { paymentId = paymentId, transactionId = transactionId, status = status });
            }
            else
            {
                return RedirectToAction("NagadCallback", new { paymentId = paymentId, transactionId = transactionId, status = status });
            }
        }

        /// <summary>
        /// bKash payment callback handler.
        /// Marks the payment as completed, approves the rental application, and notifies both parties.
        /// </summary>
        /// <param name="paymentId">The payment record to finalize.</param>
        /// <param name="transactionId">The generated bKash transaction reference ID.</param>
        /// <param name="status">Gateway response status ("success" or "failed").</param>
        [HttpGet]
        public async Task<IActionResult> bKashCallback(int paymentId, string transactionId, string status)
        {
            if (status == "success")
            {
                var success = await _paymentService.CompletePaymentAsync(paymentId, transactionId, null);
                if (success)
                {
                    TempData["Success"] = "bKash payment successful! Your booking is confirmed. 🎉";
                    return RedirectToAction("Dashboard", "UserDashboard");
                }
            }
            TempData["Error"] = "bKash payment failed or could not be verified. Please try again.";
            return RedirectToAction("Dashboard", "UserDashboard");
        }

        /// <summary>
        /// Nagad payment callback handler.
        /// Marks the payment as completed, approves the rental application, and notifies both parties.
        /// </summary>
        /// <param name="paymentId">The payment record to finalize.</param>
        /// <param name="transactionId">The generated Nagad transaction reference ID.</param>
        /// <param name="status">Gateway response status ("success" or "failed").</param>
        [HttpGet]
        public async Task<IActionResult> NagadCallback(int paymentId, string transactionId, string status)
        {
            if (status == "success")
            {
                var success = await _paymentService.CompletePaymentAsync(paymentId, transactionId, null);
                if (success)
                {
                    TempData["Success"] = "Nagad payment successful! Your booking is confirmed. 🎉";
                    return RedirectToAction("Dashboard", "UserDashboard");
                }
            }
            TempData["Error"] = "Nagad payment failed or could not be verified. Please try again.";
            return RedirectToAction("Dashboard", "UserDashboard");
        }

        /// <summary>
        /// Displays the OTP verification form for non-mobile-banking payment methods.
        /// </summary>
        /// <param name="paymentId">The payment pending OTP verification.</param>
        /// <param name="phone">The phone number the OTP was sent to.</param>
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

        /// <summary>
        /// Confirms OTP and finalizes the payment for non-mobile-banking methods.
        /// Validates the OTP against the stored value, then completes the payment.
        /// </summary>
        /// <param name="paymentId">The payment to confirm.</param>
        /// <param name="transactionId">Manual transaction reference entered by user.</param>
        /// <param name="otpCode">The OTP code entered by the user for verification.</param>
        /// <param name="phone">The phone number used for OTP (for re-display on error).</param>
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
