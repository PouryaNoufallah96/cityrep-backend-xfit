using XFit.Utilities.Attributes;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class UpsertGymTrendsUpdate
    {
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymId { get; set; }
        public List<GymTrendInfoUpdate> Trends { get; set; } = null;

    }

    public class UpsertGymTrendsUpdateByAdmin : UpsertGymTrendsUpdate
    {
        [StringInputValidation(maxLength: 32, minLength: 32)] public string GymOwnerPublicKey { get; set; }

    }

}
