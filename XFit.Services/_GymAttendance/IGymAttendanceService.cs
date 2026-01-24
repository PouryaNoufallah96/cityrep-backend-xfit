using XFit.Services._Gym.DTOs.Updates;
using XFit.Services._GymAttendance.DTOs;
using XFit.Utilities.MongoDatabase.Filter;

namespace XFit.Services._GymAttendance
{
    public interface IGymAttendanceService
    {
        #region  Client
        Task<string> CreateGymAttendanceByClientAsync(CreateGymAttendanceUpdate update, string whois);
        Task<GetClientGymAttendanceListResult> GetClientGymAttendanceListAsync(GetClientGymAttendanceListUpdate update,string whois);

        Task<bool> UpsertRateToAttendanceAsync(AddRateUpdate update,string whois);

        #endregion


        #region GymOwner

        Task<bool> VerifyGymAttendaceByGymOwnerAsync(VerifyGymAttendaceByGymOwnerUpdate update, string whois);
        Task<GetGymOwnerGymAttendanceListResult> GetGymOwnerGymAttendanceListAsync(GetGymOwnerGymAttendanceListUpdate update, string whois);


        #endregion


        #region Admin 

        Task<MonjoFilteredResult<GetGymOwnerGymAttendanceResult>> GetAllForAdminAsync(MonjoQuery query);

        #endregion

        Task ExpireAttendanceAsync();

    }
}
