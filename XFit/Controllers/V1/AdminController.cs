using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using XFit.Services._Admin;
using XFit.Services._Admin.DTOs;
using XFit.Utilities.Api;
using XFit.Utilities.Attributes;
using XFit.Utilities.Filters;

namespace XFit.Controllers.V1
{
    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AdminController(IAdminService _adminService) : ApiBaseController
    {
        #region Auth

        [HttpPost("[action]")]
        [CustomRateLimit(
            message: "تعداد درخواست های متوالی زیادی داده شده است. لطفا 5 دقیقه بعد دوباره امتحان کنید",
            maxAttemptsCount: 10,
            lockoutDurationMinutes: 5)]
        [SwaggerOperation(
            Summary = "Admin login",
            Tags = ["AdminAuth"])]
        public async Task<ActionResult> LoginAsync(
            LoginUpdate update)
            => await _adminService.LoginAsync(update);


        [HttpPut("[action]")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Reset user password by admin",
            Tags = ["Admin"])]
        public async Task<bool> ResetPasswordAsync(
            ResetPasswordUpdate update)
            => await _adminService.ResetPasswordAsync(update);

        #endregion
    }
}
