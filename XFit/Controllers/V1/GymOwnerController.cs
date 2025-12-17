using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using XFit.Services._Common.DTOs;
using XFit.Services._GymOwner;
using XFit.Services._GymOwner.DTOs.Results;
using XFit.Services._GymOwner.DTOs.Updates;
using XFit.Utilities.Api;
using XFit.Utilities.Attributes;
using XFit.Utilities.Filters;

namespace XFit.Controllers.V1
{
    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class GymOwnerController(IGymOwnerService _gymOwnerService) : ApiBaseController
    {

        #region Auth

        [HttpPost("[action]")]
        [CustomRateLimit(
            message: "تعداد درخواست های متوالی زیادی داده شده است. لطفا 5 دقیقه بعد دوباره امتحان کنید",
            maxAttemptsCount: 15,
            lockoutDurationMinutes: 5)]
        [SwaggerOperation(
            Summary = "Request a verification code for gym owner authentication",
            Tags = ["GymOwnerAuth"])]
        public async Task<bool> GetVerificationCodeForAuthenticationAsync(
            GetVerificationCodeForAuthenticationUpdate update)
            => await _gymOwnerService.GetVerificationCodeForAuthenticationAsync(update);


        [HttpPost("[action]")]
        [CustomRateLimit(
            message: "تعداد درخواست های متوالی زیادی داده شده است. لطفا 5 دقیقه بعد دوباره امتحان کنید",
            maxAttemptsCount: 15,
            lockoutDurationMinutes: 5)]
        [SwaggerOperation(
            Summary = "Login gym owner using verification code",
            Tags = ["GymOwnerAuth"])]
        public async Task<ActionResult> VerifyAndLoginWithVerificationCodeAsync(
            VerifyAndLoginWithVerificationCodeUpdate update)
            => await _gymOwnerService.VerifyAndLoginWithVerificationCodeAsync(update);


        [HttpGet("[action]")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Renew gym owner access token",
            Tags = ["GymOwnerAuth"])]
        public async Task<ActionResult> RenewTokenAsync()
            => await _gymOwnerService.RenewTokenAsync(
                JwtToken.ToString(),
                PublicKey,
                Role);

        #endregion


        #region Profile

        [HttpGet("[action]")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Get authenticated gym owner profile",
            Tags = ["GymOwnerProfile"])]
        public async Task<GymOwnerResult> GetGymOnwerDataAsync()
            => await _gymOwnerService.GetGymOnwerDataAsync(PublicKey);


        [HttpPut("[action]")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Create or update gym owner profile data",
            Tags = ["GymOwnerProfile"])]
        public async Task<GymOwnerResult> UpsertProfileDataAsync(
            GymOwnerProfileDataUpdate update)
            => await _gymOwnerService.UpsertProfileDataAsync(update, PublicKey);


        [HttpPut("[action]")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Create or update gym owner identity documents",
            Tags = ["GymOwnerProfile"])]
        public async Task<GymOwnerResult> UpsertIdentityDocumentsAsync(
            GymOwnerProfileIdentityDocumenDataUpdate update)
            => await _gymOwnerService.UpsertIdentityDocumentsAsync(update, PublicKey);


        [HttpPut("[action]")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Request to change gym owner phone number",
            Tags = ["GymOwnerProfile"])]
        public async Task<bool> RequestChangePhoneNumberAsync(
            ChangePhoneNumberUpdate update)
            => await _gymOwnerService.RequestChangePhoneNumberAsync(update, PublicKey);


        [HttpPut("[action]")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Verify gym owner phone number change request",
            Tags = ["GymOwnerProfile"])]
        public async Task<bool> VerifyChangePhoneNumberAsync(
            VerifyChangePhoneNumberUpdate update)
            => await _gymOwnerService.VerifyChangePhoneNumberAsync(update, PublicKey);

        #endregion

    }
}
