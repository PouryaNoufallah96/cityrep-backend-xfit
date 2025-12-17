using Xfit.Domain.Collections;
using XFit.Services._GymFacility.DTOs;
using XFit.Utilities.MongoDatabase.Filter;

namespace XFit.Services._GymFacility
{
    public interface IGymFacilityService
    {
        Task<GymFacility> CreateAsync(CreateGymFacilityUpdate model);
        Task<GymFacility> EditAsync(EditGymFacilityUpdate model);
        Task RemoveAsync(RemoveGymFacilityUpdate model);
        Task<GymFacility> GetByIdAsync(GymFacilityIdUpdate update);
        Task<MonjoFilteredResult<GymFacility>> GetAllAsync(MonjoQuery query);

    }
} 