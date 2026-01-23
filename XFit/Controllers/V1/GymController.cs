using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using XFit.Services._Gym;
using XFit.Services._Gym.DTOs.Results;
using XFit.Services._Gym.DTOs.Updates;
using XFit.Utilities.Api;
using XFit.Utilities.Attributes;
using XFit.Utilities.Filters;
using XFit.Utilities.MongoDatabase.Filter;

namespace XFit.Controllers.V1
{

    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class GymController(IGymService _gymService) : ApiBaseController
    {

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Add a new gym", Tags = ["GO-Gym"])]
        public async Task<GymResult> AddGymAsync(AddGymUpdate update)
        => await _gymService.AddGymAsync(update, PublicKey);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Edit an existing gym", Tags = ["GO-Gym"])]
        public async Task<GymResult> EditGymAsync(EditGymUpdate update)
            => await _gymService.EditGymAsync(update, PublicKey);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Get all gyms for owner", Tags = ["GO-Gym"])]
        public async Task<GymListResult> GetAllGymsAsync(GymSimpleFilter filter)
            => await _gymService.GetAllGymsAsync(filter, PublicKey);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Upsert gym trend data", Tags = ["GO-Gym"])]
        public async Task<GymResult> UpsertGymTrendsAsync(UpsertGymTrendsUpdate update)
            => await _gymService.UpsertGymTrendsAsync(update, PublicKey);


        // ===== Client Side =====

        [CustomRateLimit]
        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Get one gym by id", Tags = ["C-Gym"])]
        public async Task<GymResult> GetOneGymAsync(GymIdUpdate update)
            => await _gymService.GetOneGymAsync(update);

        [CustomRateLimit]
        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Get gyms with filters", Tags = ["C-Gym"])]
        public async Task<GymListLightResult> GetGymsWithFilterAsync(GymFilter update)
            => await _gymService.GetGymsWithFilterAsync(update);


        [CustomRateLimit]
        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Get gym data by slug", Tags = ["C-Gym"])]
        public async Task<GymFullResult> GetGymDataBySlugAsync(string slug)
            => await _gymService.GetGymDataBySlugAsync(slug);


        // ===== Admin Side =====

        [CustomRateLimit]
        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Get all gyms for admin", Tags = ["A-Gym"])]
        public async Task<MonjoFilteredResult<GymAdminResult>> GetAllGymsForAdminAsync(MonjoQuery query)
            => await _gymService.GetAllGymsForAdminAsync(query);

        [CustomRateLimit]
        [HttpPost("[action]")]
        //[Authorize]
        [SwaggerOperation(Summary = "Add a gym by admin", Tags = ["A-Gym"])]
        public async Task<GymAdminResult> AddGymByAdminAsync(AddGymByAdminUpdate update)
            => await _gymService.AddGymByAdminAsync(update);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Edit a gym by admin", Tags = ["A-Gym"])]
        public async Task<GymAdminResult> EditGymByAdminAsync(EditGymByAdminUpdate update)
            => await _gymService.EditGymByAdminAsync(update);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "remove a gym by admin", Tags = ["A-Gym"])]
        public async Task<GymAdminResult> RemoveGymByAdminAsync(GymIdUpdate gymIdUpdate)
            => await _gymService.RemoveGymByAdminAsync(gymIdUpdate);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "upsert gym trends data by admin", Tags = ["A-Gym"])]
        public async Task<GymAdminResult> UpsertGymTrendsByAdminAsync(UpsertGymTrendsUpdateByAdmin update)
            => await _gymService.UpsertGymTrendsByAdminAsync(update);

    }
}
