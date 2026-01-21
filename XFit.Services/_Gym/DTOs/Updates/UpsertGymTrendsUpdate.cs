namespace XFit.Services._Gym.DTOs.Updates
{
    public class UpsertGymTrendsUpdate
    {
        public string GymId { get; set; }
        public List<GymTrendInfoUpdate> Trends { get; set; } = null;

    }

    public class UpsertGymTrendsUpdateByAdmin : UpsertGymTrendsUpdate
    {
        public string GymOwnerPublicKey { get; set; }

    }

}
