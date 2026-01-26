using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Xfit.Domain.Collections;
using XFit.Services._GymTrend;
using XFit.Services._GymTrend.DTOs;
using XFit.Utilities.Api;
using XFit.Utilities.Attributes;
using XFit.Utilities.Filters;
using XFit.Utilities.MongoDatabase.Filter;
using XFit.Utilities.Permissions;

namespace XFit.Controllers.V1
{

    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class GymTrendController(IGymTrendService _gymTrendService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize(Permissions.CreateUser)]
        [SwaggerOperation(Summary = "Create a gym trend", Tags = ["A-Trend"])]
        public async Task<GymTrend> CreateAsync(CreateGymTrendUpdate update)
        => await _gymTrendService.CreateAsync(update);


        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize(Permissions.CreateUser)]
        [SwaggerOperation(Summary = "Edit a gym trend", Tags = ["A-Trend"])]
        public async Task<GymTrend> EditAsync(EditGymTrendUpdate update)
            => await _gymTrendService.EditAsync(update);


        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize(Permissions.CreateUser)]
        [SwaggerOperation(Summary = "Remove a gym trend", Tags = ["A-Trend"])]
        public async Task<bool> RemoveAsync(RemoveGymTrendUpdate update)
        {
            await _gymTrendService.RemoveAsync(update);
            return true;
        }


        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize(Permissions.CreateUser)]
        [SwaggerOperation(Summary = "Get a gym trend by id", Tags = ["A-Trend"])]
        public async Task<GymTrend> GetByIdAsync(GymTrendIdUpdate update)
            => await _gymTrendService.GetByIdAsync(update);


        [CustomRateLimit]
        [HttpPost("[action]")]
        [Authorize(Permissions.CreateUser)]
        [SwaggerOperation(Summary = "Get gym trends list", Tags = ["A-Trend"])]
        public async Task<MonjoFilteredResult<GymTrend>> GetAllAsync(MonjoQuery query)
            => await _gymTrendService.GetAllAsync(query);

    }
}
