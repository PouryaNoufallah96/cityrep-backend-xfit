using XFit.Services._Report.DTOs.Results;

namespace XFit.Services._Report
{
    public interface IReportService
    {
        Task<GymOwnerOverviewResult> GetGymOwnerOverviewAsync(string gymOwner);
        Task<List<GymTrendCapacityOverviewResult>> GetGymTrendCapacityOverviewAsync(string gymOwnerPublicKey);
        Task<List<WeeklyReservationResult>> GetCurrentWeekReservationsAsync(string gymOwner);
    }
}
