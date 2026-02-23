namespace XFit.Services._Report.DTOs.Results
{
    public class GymOwnerOverviewResult
    {
        public int TotalReserved { get; set; } = 0;
        public int TotalActiveReserved { get; set; } = 0;
        public decimal TotalIncome { get; set; } = 0m;
    }

    public class WeeklyReservationResult
    {
        public DayOfWeek DayOfWeek { get; set; }
        public int Count { get; set; }
    }


    public class GymTrendCapacityOverviewResult
    {
        public string GymTrendId { get; set; }
        public string GymTrendTitle { get; set; }
        public int TotalCapacity { get; set; }
        public int TotalUsedCapacity { get; set; }
    }

}
