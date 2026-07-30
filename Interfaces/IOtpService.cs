namespace BachelorRoomFinding.Interfaces
{
    public interface IOtpService
    {
        Task<string> GenerateAndSendOtpAsync(string phoneNumber);
        Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode);
    }
}
