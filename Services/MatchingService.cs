using BachelorRoomFinding.Entities;

namespace BachelorRoomFinding.Services
{
    public class MatchingService
    {
        public static int CalculateCompatibility(UserPreference p1, UserPreference p2)
        {
            if (p1 == null || p2 == null) return 50;

            int score = 0;
            int totalPoints = 0;

            // Smoking (Weighted more)
            totalPoints += 20;
            if (p1.Smoking == p2.Smoking) score += 20;
            else if (p1.Smoking == "Non-Smoker" || p2.Smoking == "Non-Smoker") score += 0;
            else score += 10; // Both are smokers but different types

            // Sleep Schedule
            totalPoints += 15;
            if (p1.SleepSchedule == p2.SleepSchedule) score += 15;
            else if (p1.SleepSchedule == "Flexible" || p2.SleepSchedule == "Flexible") score += 10;

            // Cleanliness
            totalPoints += 15;
            if (p1.Cleanliness == p2.Cleanliness) score += 15;
            else if (Math.Abs(GetCleanLevel(p1.Cleanliness) - GetCleanLevel(p2.Cleanliness)) <= 1) score += 8;

            // Food Habit
            totalPoints += 10;
            if (p1.FoodHabit == p2.FoodHabit || p1.FoodHabit == "Both" || p2.FoodHabit == "Both") score += 10;

            // Prayer Habit
            totalPoints += 10;
            if (p1.PrayerHabit == p2.PrayerHabit) score += 10;

            // Guest Policy
            totalPoints += 15;
            if (p1.GuestPolicy == p2.GuestPolicy) score += 15;
            else if (p1.GuestPolicy == "Restricted" || p2.GuestPolicy == "Restricted") score += 7;

            // Pet Friendly
            totalPoints += 15;
            if (p1.PetFriendly == p2.PetFriendly) score += 15;

            return (int)((double)score / totalPoints * 100);
        }

        private static int GetCleanLevel(string level) => level switch
        {
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 2
        };
    }
}
