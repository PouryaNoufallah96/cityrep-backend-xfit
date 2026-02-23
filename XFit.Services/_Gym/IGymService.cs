using Xfit.Domain.Collections;
using XFit.Services._Gym.DTOs.Results;
using XFit.Services._Gym.DTOs.Updates;
using XFit.Utilities.MongoDatabase.Filter;

namespace XFit.Services._Gym
{
    public interface IGymService
    {

        // gymOnwer side
        Task<GymResult> AddGymAsync(AddGymUpdate update, string gymOwnerPublicKey);
        Task<GymResult> EditGymImagesAsync(EditGymImagesUpdate update, string gymOwnerPublicKey); 
        Task<GymResult> EditGymCommonDataAsync(EditGymCommonDataUpdate update, string gymOwnerPublicKey); 
        Task<GymResult> EditGymGeoLocationsync(EditGymGeoLocationUpdate update, string gymOwnerPublicKey);

        Task<GymResult> AddSessionForGymTrendAsync(AddSessionForGymTrendUpdate update,string gymOwnerPublicKey);
        Task<GymResult> RemoveSessionForGymTrendAsync(RemoveGymSessionUpdate update,string gymOwnerPublicKey);

        Task<GymResult> ActiveOrDeactiveGymTrendAsync(DeactiveGymTrendUpdate update, string gymOwnerPublicKey);
        Task<GymResult> ActiveOrDeactiveGymSessionAsync(DeactiveGymSessionUpdate update, string gymOwnerPublicKey);

        Task<GymSessionsListResult> GetGymSessionsListAsync(GymSessionsListUpdate update,string gymOwnerPublicKey);

        Task<GymResult> EditGymAsync(EditGymUpdate update, string gymOwnerPublicKey);
        Task<GymListResult> GetAllGymsAsync(GymSimpleFilter filter, string whois);
        Task<GymResult> UpsertGymTrendsAsync(UpsertGymTrendsUpdate update, string whois);


        //client
        Task<GymResult> GetOneGymAsync(GymIdUpdate update);
        Task<GymListLightResult> GetGymsWithFilterAsync(GymFilter update);
        Task<GymFullResult> GetGymDataBySlugAsync(string slug);


        //admin side
        Task<MonjoFilteredResult<GymAdminResult>> GetAllGymsForAdminAsync(MonjoQuery query);
        Task<GymAdminResult> AddGymByAdminAsync(AddGymByAdminUpdate update);
        Task<GymAdminResult> EditGymByAdminAsync(EditGymByAdminUpdate update);
        Task<GymAdminResult> RemoveGymByAdminAsync(GymIdUpdate gymIdUpdate);
        //Task<GymAdminResult> UpsertGymTrendsByAdminAsync(UpsertGymTrendsUpdateByAdmin update);

        // internal 
        Task<Gym> GetOneGymForInternalUsageAsync(string gymId);
        Task<bool> IsFacilityUsedAsync(string facilityId);
        Task UpdateFacilityTitleAsync(string facilityId, string newTitle);
        Task<bool> IsTrendUsedAsync(string trendId);
        Task UpdateGymTrendTitleAsync(string trendId, string newTitle);

        Task SyncRateOfGymAsync(string gymId);

        Task IncreaseSessionAvailableCapacityAsync(
            string gymId,
            string gymSessionId,
            bool isMenSession);

        Task IncreaseSessionAvailableCapacityAsync(
         Gym gym,
         GymSession gymSession,
         bool isMenSession);

        Task UpdateGymsWithFacilityAsync(GymFacility updatedFacility);
        Task UpdateGymsWithTrendAsync(GymTrend updatedTrend);

        Task UndoGymCapacityByAttendanceAsync(string depositReference);
        Task MakeDoneAttendanceAsync(string depositReference);
    }
}
