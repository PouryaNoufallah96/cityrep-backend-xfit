using Xfit.Domain.Collections;
using XFit.Services._GymTrend.DTOs;
using XFit.Utilities.MongoDatabase.Filter;

namespace XFit.Services._GymTrend
{
    public interface IGymTrendService
    {
        Task<GymTrend> CreateAsync(CreateGymTrendUpdate update);
        Task<GymTrend> EditAsync(EditGymTrendUpdate update);
        Task RemoveAsync(RemoveGymTrendUpdate update);
        Task<GymTrend> GetByIdAsync(GymTrendIdUpdate update);
        Task<MonjoFilteredResult<GymTrend>> GetAllAsync(MonjoQuery query);
        Task<List<GymTrend>> GetAllAsync();
    }
}
