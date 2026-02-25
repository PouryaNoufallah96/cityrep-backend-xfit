using XFit.Utilities.Attributes;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class AddSessionForGymTrendUpdate
    {
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymId { get; set; }
        [ObjectInputValidation] public GymTrendInfoUpdate TrendData { get; set; } 
    }
    public class AddTrendToGymUpdate
    {
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymId { get; set; }
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymTrendId { get; set; }
    }


    public class RemoveGymSessionUpdate
    {
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymId { get; set; }
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymTrendId { get; set; }
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymSessionId { get; set; }
    }
}
