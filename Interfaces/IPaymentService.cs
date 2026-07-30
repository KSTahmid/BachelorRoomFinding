using BachelorRoomFinding.Entities;

namespace BachelorRoomFinding.Interfaces
{
    public interface IPaymentService
    {
        Task<Payment> InitializePaymentAsync(int applicationId, int userId, string method, decimal amount, string? senderWalletNumber = null);
        Task<bool> CompletePaymentAsync(int paymentId, string transactionId, string? otpCode);
    }
}
