using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Admin;
using XFit.Services._Admin.DTOs;
using XFit.Utilities.Api;
using XFit.Utilities.Attributes;
using XFit.Utilities.Filters;
using XFit.Utilities.Services.Contracts;

namespace XFit.Controllers.V1
{
    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AdminController(IAdminService _adminService, IAdminRepository adminRepository, IPasswordService passwordService    ) : ApiBaseController
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
        [CustomRateLimit]
        [SwaggerOperation(
            Summary = "Reset user password by admin",
            Tags = ["Admin"])]
        public async Task<bool> ResetPasswordAsync(
            ResetPasswordUpdate update)
            => await _adminService.ResetPasswordAsync(update);


        //public async Task<bool> CreateTempAdmin()
        //{
        //    var newA = new Admin
        //    { 
        //        UserName = "superadmin",
        //        PasswordHash = passwordService.Hash("12341234"),
        //        Role= Xfit.Domain.Common.UserRole.Admin,
        //        Status = Xfit.Domain.Common.UserStatus.Active
        //    };
        //    await adminRepository.InsertOneAsync(newA);
        //    return true;
        //}
          

        #endregion
    }
}
