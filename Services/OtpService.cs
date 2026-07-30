using BachelorRoomFinding.Interfaces;

namespace BachelorRoomFinding.Services
{
    public class OtpService : IOtpService
    {
        // Simple in-memory dictionary to store OTPs temporarily. In production, use Redis or DB table.
        private static readonly Dictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();

        public Task<string> GenerateAndSendOtpAsync(string phoneNumber)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            _otpStore[phoneNumber] = (otp, DateTime.Now.AddMinutes(5));
            
            // Console print for development simulation (acting like Twilio/SSL Wireless)
            Console.WriteLine($"\n[OTP SIMULATOR] Sent OTP {otp} to {phoneNumber}\n");
            
            return Task.FromResult(otp);
        }

        public Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode)
        {
            if (_otpStore.TryGetValue(phoneNumber, out var data))
            {
                if (data.Otp == otpCode && DateTime.Now <= data.Expiry)
                {
                    _otpStore.Remove(phoneNumber);
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }
}
