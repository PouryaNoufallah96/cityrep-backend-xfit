using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Xfit.Domain.Collections;
using XFit.Services._GymFacility;
using XFit.Services._GymFacility.DTOs;
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
    public class GymFacilityController(IGymFacilityService _gymFacilityService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Create a new gym facility", Tags = ["Gym Facility Admin"])]
        public async Task<GymFacility> CreateAsync(CreateGymFacilityUpdate update)
            => await _gymFacilityService.CreateAsync(update);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Edit an existing gym facility", Tags = ["Gym Facility Admin"])]
        public async Task<GymFacility> EditAsync(EditGymFacilityUpdate update)
            => await _gymFacilityService.EditAsync(update);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Remove a gym facility", Tags = ["Gym Facility Admin"])]
        public async Task<bool> RemoveAsync(RemoveGymFacilityUpdate update)
        {
            await _gymFacilityService.RemoveAsync(update);
            return true;
        }

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Get gym facility by id", Tags = ["Gym Facility Admin"])]
        public async Task<GymFacility> GetByIdAsync(GymFacilityIdUpdate update)
            => await _gymFacilityService.GetByIdAsync(update);


        [CustomRateLimit]
        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Get all gym facilities (filtered)", Tags = ["Gym Facility Admin"])]
        public async Task<MonjoFilteredResult<GymFacility>> GetAllAsync(MonjoQuery query)
            => await _gymFacilityService.GetAllAsync(query);
    }
}
