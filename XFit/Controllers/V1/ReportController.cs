using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using XFit.Services._Report;
using XFit.Services._Report.DTOs.Results;
using XFit.Utilities.Api;
using XFit.Utilities.Attributes;
using XFit.Utilities.Filters;

namespace XFit.Controllers.V1
{
    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ReportController(IReportService _reportService) : ApiBaseController
    {
        [HttpGet("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Get gym owner overview report", Tags = ["GO-Report"])]
        public async Task<GymOwnerOverviewResult> GetGymOwnerOverviewAsync()
        => await _reportService.GetGymOwnerOverviewAsync(PublicKey);


        [HttpGet("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Get gym trend capacity overview", Tags = ["GO-Report"])]
        public async Task<List<GymTrendCapacityOverviewResult>> GetGymTrendCapacityOverviewAsync()
            => await _reportService.GetGymTrendCapacityOverviewAsync(PublicKey);


        [HttpGet("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "Get current week reservations", Tags = ["GO-Report"])]
        public async Task<List<WeeklyReservationResult>> GetCurrentWeekReservationsAsync()
            => await _reportService.GetCurrentWeekReservationsAsync(PublicKey);
    }
}
