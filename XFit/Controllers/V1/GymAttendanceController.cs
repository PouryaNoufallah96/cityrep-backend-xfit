using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using XFit.Services._Gym.DTOs.Updates;
using XFit.Services._GymAttendance;
using XFit.Services._GymAttendance.DTOs;
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
    public class GymAttendanceController(IGymAttendanceService _gymAttendanceService) : ApiBaseController
    {
        #region Client

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Create gym attendance by client", Tags = ["C-Attendance"])]
        public async Task<CreateGymAttendanceByClientResult> CreateByClientAsync(CreateGymAttendanceUpdate update)
            => await _gymAttendanceService.CreateGymAttendanceByClientAsync(update, PublicKey);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Get client gym attendance list", Tags = ["C-Attendance"])]
        public async Task<GetClientGymAttendanceListResult> GetClientListAsync(GetClientGymAttendanceListUpdate update)
            => await _gymAttendanceService.GetClientGymAttendanceListAsync(update, PublicKey);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Add or update rate for gym attendance", Tags = ["C-Attendance"])]
        public async Task<bool> UpsertRateAsync(AddRateUpdate update)
            => await _gymAttendanceService.UpsertRateToAttendanceAsync(update, PublicKey);

        #endregion


        #region GymOwner

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Verify gym attendance by gym owner", Tags = ["GO-Attendance"])]
        public async Task<bool> VerifyByGymOwnerAsync(VerifyGymAttendaceByGymOwnerUpdate update)
            => await _gymAttendanceService.VerifyGymAttendaceByGymOwnerAsync(update, PublicKey);

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Get gym owner attendance list", Tags = ["GO-Attendance"])]
        public async Task<GetGymOwnerGymAttendanceListResult> GetGymOwnerListAsync(GetGymOwnerGymAttendanceListUpdate update)
            => await _gymAttendanceService.GetGymOwnerGymAttendanceListAsync(update, PublicKey);

        #endregion


        #region Admin

        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Get all gym attendances for admin", Tags = ["A-Attendance"])]
        public async Task<MonjoFilteredResult<GetGymOwnerGymAttendanceResult>> GetAllForAdminAsync(MonjoQuery query)
            => await _gymAttendanceService.GetAllForAdminAsync(query);

        #endregion
    }
}
