using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BachelorRoomFinding.Entities
{
    public class UserPreference
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        // Lifestyle Preferences
        public string Smoking { get; set; } = "Non-Smoker"; // Non-Smoker, Occasional, Regular
        public string SleepSchedule { get; set; } = "Night Owl"; // Night Owl, Early Bird, Flexible
        public string Cleanliness { get; set; } = "Medium"; // High, Medium, Low
        public string FoodHabit { get; set; } = "Non-Veg"; // Veg, Non-Veg, Both
        public string PrayerHabit { get; set; } = "Regular"; // Regular, Occasional, None
        public string GuestPolicy { get; set; } = "Restricted"; // Open, Restricted, No-Guests
        public string PetFriendly { get; set; } = "No"; // Yes, No, Only-Small
        
        [ValidateNever]
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
