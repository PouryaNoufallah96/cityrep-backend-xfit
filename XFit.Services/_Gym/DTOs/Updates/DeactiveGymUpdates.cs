using XFit.Utilities.Attributes;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class DeactiveGymTrendUpdate
    {
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymId { get; set; }
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymTrendId { get; set; }

    }
    public class DeactiveGymSessionUpdate
    {
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymId { get; set; }
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymTrendId { get; set; }
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymSessionId { get; set; }
    } 
}
