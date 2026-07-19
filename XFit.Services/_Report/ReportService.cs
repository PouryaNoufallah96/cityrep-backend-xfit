using MongoDB.Driver.Linq;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Report.DTOs.Results;
using XFit.Utilities.Exceptions.Common;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Report
{
    public class ReportService(IGymRepository _gymRepository, IGymAttendanceRepository _gymAttendanceRepository) : IReportService, IScopedDependency
    {


        #region Gym Owner
        /// <summary>
        /// use for GO overview
        /// </summary>
        /// <param name="gymOwner"></param>
        /// <returns></returns>
        public async Task<GymOwnerOverviewResult> GetGymOwnerOverviewAsync(string gymOwner)
        {

            var result = await _gymAttendanceRepository
                .AsQueryable()
                .Where(x => x.GymOwnerPublicKey == gymOwner)
                .Where(x =>
                    x.GymAttendanceState == GymAttendanceState.Reserved ||
                    x.GymAttendanceState == GymAttendanceState.Used ||
                    x.GymAttendanceState == GymAttendanceState.NoShow)
                .GroupBy(x => 1)
                .Select(g => new GymOwnerOverviewResult
                {
                    TotalReserved = g.Count(),

                    TotalActiveReserved = g.Count(x =>
                        x.GymAttendanceState == GymAttendanceState.Reserved),

                    TotalIncome = g
                        .Where(x =>
                            x.GymAttendanceState == GymAttendanceState.Used ||
                            x.GymAttendanceState == GymAttendanceState.NoShow)
                        .Sum(x => x.SessionPrice)
                })
                .FirstOrDefaultAsync();

            return result ?? new GymOwnerOverviewResult();
        }

        /// <summary>
        /// use for get current week reservation
        /// </summary>
        /// <param name="gymOwner"></param>
        /// <returns></returns>
        public async Task<List<WeeklyReservationResult>> GetCurrentWeekReservationsAsync(string gymOwner)
        {
            var today = DateTime.UtcNow.Date;

            var startDate = today.AddDays(-6);
            var endDate = today.AddDays(1);

            var data = await _gymAttendanceRepository
                .AsQueryable()
                .Where(x =>
                    x.GymOwnerPublicKey == gymOwner &&
                    (x.GymAttendanceState == GymAttendanceState.Reserved ||
                     x.GymAttendanceState == GymAttendanceState.Used ||
                     x.GymAttendanceState == GymAttendanceState.NoShow) &&
                    x.SessionDate >= startDate &&
                    x.SessionDate < endDate)
                .GroupBy(x => x.SessionDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var result = Enumerable.Range(0, 7)
                .Select(i => startDate.AddDays(i))
                .Select(date => new WeeklyReservationResult
                {
                    DayOfWeek = date.DayOfWeek,
                    Count = data.FirstOrDefault(x => x.Date == date)?.Count ?? 0
                })
                .ToList();

            return result;
        }



        /// <summary>
        /// use for see capacity for trends fo GO
        /// </summary>
        /// <param name="gymOwnerPublicKey"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public async Task<List<GymTrendCapacityOverviewResult>> GetGymTrendCapacityOverviewAsync(string gymOwnerPublicKey)
        {
          
            var gym = await _gymRepository.AsQueryable()
                .Where(x => x.GymOwnerPublicKey == gymOwnerPublicKey)
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException("باشگاه یافت نشد");

            if (gym.Trends == null || !gym.Trends.Any())
                return new List<GymTrendCapacityOverviewResult>();

            var result = gym.Trends.Select(trend =>
            {
                var allSessions = new List<GymSession>();

                if (trend.Men != null)
                    allSessions.AddRange(
                        trend.Men.SelectMany(x => x.Sessions ?? new List<GymSession>())
                    );

                if (trend.Women != null)
                    allSessions.AddRange(
                        trend.Women.SelectMany(x => x.Sessions ?? new List<GymSession>())
                    );

                return new GymTrendCapacityOverviewResult
                {
                    GymTrendId = trend.GymTrendId,
                    GymTrendTitle = trend.Title,
                    TotalCapacity = allSessions
                        .Where(x => x.Capacity.HasValue)
                        .Sum(x => x.Capacity.Value),

                    TotalUsedCapacity = allSessions
                        .Where(x => x.UsedCapacity.HasValue)
                        .Sum(x => x.UsedCapacity.Value)
                };
            }).ToList();

            return result;
        }

        #endregion


    }

}
