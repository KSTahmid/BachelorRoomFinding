using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Services
{
    /// <summary>
    /// Service responsible for all payment business logic.
    /// Handles payment initialization (upsert), completion, and mess board auto-joining after successful rent payment.
    /// Supports bKash, Nagad, and Bank Transfer methods.
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly IOtpService _otpService;

        /// <summary>
        /// Injects the database context and OTP service.
        /// </summary>
        public PaymentService(AppDbContext context, IOtpService otpService)
        {
            _context = context;
            _otpService = otpService;
        }

        /// <summary>
        /// Creates or updates a pending payment record for the given rental application.
        /// Uses upsert logic: if a pending payment already exists for this application, it is updated
        /// rather than inserting a duplicate (which would violate the unique index constraint).
        /// </summary>
        /// <param name="applicationId">The rental application being paid for.</param>
        /// <param name="userId">The tenant's user ID making the payment.</param>
        /// <param name="method">Payment method: "bKash", "Nagad", or "BankTransfer".</param>
        /// <param name="amount">The payment amount in BDT.</param>
        /// <param name="senderWalletNumber">The tenant's wallet number (optional).</param>
        /// <returns>The initialized or updated Payment entity.</returns>
        public async Task<Payment> InitializePaymentAsync(int applicationId, int userId, string method, decimal amount, string? senderWalletNumber = null)
        {
            var application = await _context.RentalApplications
                .Include(a => a.Room)
                .ThenInclude(r => r.Owner)
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            if (application == null) throw new ArgumentException("Application not found");

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.ApplicationId == applicationId);
            
            if (payment != null)
            {
                if (payment.Status == "Completed") return payment;
                
                payment.Method = method;
                payment.Amount = amount;
                payment.SenderWalletNumber = senderWalletNumber;
                payment.RecipientWalletNumber = method == "Nagad"
                    ? application.Room.Owner.NagadNumber ?? application.Room.Owner.PhoneNumber
                    : application.Room.Owner.BkashNumber ?? application.Room.Owner.PhoneNumber;
                payment.Status = "Pending";
                payment.CreatedAt = DateTime.Now;
            }
            else
            {
                payment = new Payment
                {
                    ApplicationId = applicationId,
                    UserId = userId,
                    OwnerId = application.Room.OwnerId,
                    RoomId = application.RoomId,
                    Method = method,
                    Amount = amount,
                    SenderWalletNumber = senderWalletNumber,
                    RecipientWalletNumber = method == "Nagad"
                        ? application.Room.Owner.NagadNumber ?? application.Room.Owner.PhoneNumber
                        : application.Room.Owner.BkashNumber ?? application.Room.Owner.PhoneNumber,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };
                _context.Payments.Add(payment);
            }

            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> CompletePaymentAsync(int paymentId, string transactionId, string? otpCode)
        {
            var payment = await _context.Payments.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null) return false;
            if (payment.Status == "Completed") return true;

            var duplicateTransaction = await _context.Payments.AnyAsync(p =>
                p.Id != paymentId &&
                p.TransactionId == transactionId &&
                !string.IsNullOrEmpty(transactionId));
            if (duplicateTransaction) return false;

            if (payment.Method != "bKash" && payment.Method != "Nagad")
            {
                if (payment.User == null) return false;
                var phoneNumber = payment.User.PhoneNumber;
                if (string.IsNullOrEmpty(phoneNumber)) return false;

                var isValidOtp = await _otpService.VerifyOtpAsync(phoneNumber, otpCode ?? "");
                if (!isValidOtp) return false;

                payment.IsOtpVerified = true;
                payment.OtpCode = otpCode;
            }
            else
            {
                payment.IsOtpVerified = false;
            }

            payment.TransactionId = transactionId;
            payment.Status = "Completed";
            payment.VerifiedAt = DateTime.Now;

            await using var tx = await _context.Database.BeginTransactionAsync();

            var application = await _context.RentalApplications
                .Include(a => a.Room)
                .FirstOrDefaultAsync(a => a.Id == payment.ApplicationId);
            
            if (application != null && application.Room != null)
            {
                application.Status = ApplicationStatus.Approved;
                application.ReviewedAt ??= DateTime.Now;
                application.Room.IsAvailable = false;
                application.Room.Status = RoomStatus.Rented;
                payment.OwnerId = application.Room.OwnerId;
                payment.RoomId = application.RoomId;

                _context.Notifications.Add(new Notification
                {
                    UserId              = payment.UserId ?? 0,
                    Title               = "Payment Confirmed!",
                    NotificationMessage = $"Your payment for \"{application.Room.Title}\" has been confirmed via {payment.Method}. Welcome to your new room!",
                    Type                = NotificationType.PaymentConfirmed,
                    IsRead              = false,
                    CreatedAt           = DateTime.Now
                });

                _context.Notifications.Add(new Notification
                {
                    UserId              = application.Room.OwnerId,
                    Title               = "Rent Payment Received",
                    NotificationMessage = $"Payment for \"{application.Room.Title}\" was completed via {payment.Method}.",
                    Type                = NotificationType.PaymentConfirmed,
                    IsRead              = false,
                    CreatedAt           = DateTime.Now
                });

                await EnsureTenantJoinedMessBoardAsync(application.RoomId, payment.UserId ?? application.ApplicantId);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }

        private async Task EnsureTenantJoinedMessBoardAsync(int roomId, int tenantUserId)
        {
            var group = await _context.MessGroups.FirstOrDefaultAsync(g => g.RoomId == roomId);
            if (group == null) return;

            var exists = await _context.MessMembers.AnyAsync(m =>
                m.MessGroupId == group.Id && m.UserId == tenantUserId);
            if (exists) return;

            _context.MessMembers.Add(new MessMember
            {
                MessGroupId = group.Id,
                UserId = tenantUserId,
                Role = MessRole.Tenant,
                IsManager = false,
                JoinedAt = DateTime.Now
            });
        }
    }
}
