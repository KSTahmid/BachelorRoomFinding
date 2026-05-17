using System.ComponentModel.DataAnnotations;

namespace BachelorRoomFinding.ViewModels
{
    public class ApplicationViewModel
    {
        public int RoomId { get; set; }

        [Required]
        public DateTime MoveInDate { get; set; } = DateTime.Today.AddDays(7);

        [Required, Range(1, 24)]
        public int DurationMonths { get; set; } = 1;

        public string? Message { get; set; }
    }

    public class PaymentViewModel
    {
        public int ApplicationId { get; set; }

        [Required]
        public string Method { get; set; } = "bKash"; // bKash | Nagad | BankTransfer

        [Required, Range(1, 10000000)]
        public decimal Amount { get; set; }

        public string? TransactionId { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
    }

    public class KycViewModel
    {
        [Required]
        public string NationalIdNumber { get; set; } = string.Empty;

        public IFormFile? NidFrontFile { get; set; }
        public IFormFile? NidBackFile { get; set; }
        public IFormFile? FacePhotoFile { get; set; }
    }

    public class ReviewViewModel
    {
        public int RoomId { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }
}
