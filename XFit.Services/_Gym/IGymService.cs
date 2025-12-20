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
        Task<GymResult> EditGymAsync(EditGymUpdate update, string gymOwnerPublicKey);
        Task<GymListResult> GetAllGymsAsync(string whois);
         


        //client
        Task<GymResult> GetOneGymAsync();
        Task<GymListResult> GetGymsWithFilterAsync(GymFilter update);


        //admin side
        Task<MonjoFilteredResult<Gym>> GetAllGymsForAdminAsync(MonjoQuery query);
        Task<Gym> AddGymByAdminAsync(AddGymByAdminUpdate update, string whois);
        Task<Gym> EditGymByAdminAsync(EditGymByAdminUpdate update, string whois);



    }
}
