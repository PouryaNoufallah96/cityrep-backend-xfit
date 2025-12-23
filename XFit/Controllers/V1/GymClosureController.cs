using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using XFit.Services._GymClosure;
using XFit.Services._GymClosure.DTOs;
using XFit.Utilities.Api;
using XFit.Utilities.Filters;
using XFit.Utilities.MongoDatabase.Filter;

namespace XFit.Controllers.V1
{
    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class GymClosureController(IGymClosureService _gymClosureService) : ApiBaseController
    {
        #region GymOwner Side

        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Create a gym closure", Tags = new[] { "Gym Closure Owner" })]
        public async Task<GymClosureResult> CreateAsync(GymClosureCreateUpdate dto)
            => await _gymClosureService.CreateAsync(dto, PublicKey);

        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Edit a gym closure", Tags = new[] { "Gym Closure Owner" })]
        public async Task<GymClosureResult> UpdateAsync(GymClosureEditUpdate dto)
            => await _gymClosureService.UpdateAsync(dto, PublicKey);

        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Remove a gym closure", Tags = new[] { "Gym Closure Owner" })]
        public async Task<GymClosureResult> DeleteAsync(RemoveGymClosureUpdate dto)
            => await _gymClosureService.DeleteAsync(dto, PublicKey);

        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Get a gym closure by id", Tags = new[] { "Gym Closure Owner" })]
        public async Task<GymClosureResult> GetOneByIdAsync(GymClosureIdUpdate dto)
            => await _gymClosureService.GetOneByIdAsync(dto);

        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Get list of closures for gym owner", Tags = new[] { "Gym Closure Owner" })]
        public async Task<GymClosureListResult> GetListForGymOwnerAsync(GymClosureListUpdate dto)
            => await _gymClosureService.GetListForGymOwnerAsync(dto, PublicKey);

        #endregion



        #region Admin Side

        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Create a gym closure by admin", Tags = new[] { "Gym Closure Admin" })]
        public async Task<GymClosureForAdminResult> CreateByAdminAsync(GymClosureCreateForAdminUpdate dto)
            => await _gymClosureService.CreateByAdminAsync(dto);

        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Edit a gym closure by admin", Tags = new[] { "Gym Closure Admin" })]
        public async Task<GymClosureForAdminResult> UpdateByAdminAsync(GymClosureEditForAdminUpdate dto)
            => await _gymClosureService.UpdateByAdminAsync(dto);

        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Remove a gym closure by admin", Tags = new[] { "Gym Closure Admin" })]
        public async Task<GymClosureForAdminResult> DeleteByAdminAsync(RemoveGymClosureForAdminUpdate dto)
            => await _gymClosureService.DeleteByAdminAsync(dto);

        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Get a gym closure by id by admin", Tags = new[] { "Gym Closure Admin" })]
        public async Task<GymClosureForAdminResult> GetOneByIdByAdminAsync(GymClosureIdUpdate dto)
            => await _gymClosureService.GetOneByIdByAdminAsync(dto);

        [HttpPost("[action]")]
        [Authorize]
        [SwaggerOperation(Summary = "Get list of gym closures for admin", Tags = new[] { "Gym Closure Admin" })]
        public async Task<MonjoFilteredResult<GymClosureForAdminResult>> GetListForAdminAsync(MonjoQuery query)
            => await _gymClosureService.GetListForAdminAsync(query);

        #endregion
    }
}
