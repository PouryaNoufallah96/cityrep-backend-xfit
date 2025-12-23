using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using XFit.Services._Client;
using XFit.Services._Client.DTOs.Results;
using XFit.Services._Client.DTOs.Updates;
using XFit.Services._Common.DTOs;
using XFit.Utilities.Api;
using XFit.Utilities.Attributes;
using XFit.Utilities.Filters;

namespace XFit.Controllers.V1
{
    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ClientController(IClientService _clientService) : ApiBaseController
    {
        #region Auth

        [HttpPost("[action]")]
        [CustomRateLimit(
            message: "تعداد درخواست های متوالی زیادی داده شده است. لطفا 5 دقیقه بعد دوباره امتحان کنید",
            maxAttemptsCount: 15,
            lockoutDurationMinutes: 5)]
        [SwaggerOperation(
            Summary = "Request a verification code for authentication",
            Tags = ["ClientAuth"])]
        public async Task<bool> GetVerificationCodeForAuthenticationAsync(
            GetVerificationCodeForAuthenticationUpdate update)
            => await _clientService.GetVerificationCodeForAuthenticationAsync(update);


        [HttpPost("[action]")]
        [CustomRateLimit(
            message: "تعداد درخواست های متوالی زیادی داده شده است. لطفا 5 دقیقه بعد دوباره امتحان کنید",
            maxAttemptsCount: 15,
            lockoutDurationMinutes: 5)]
        [SwaggerOperation(
            Summary = "Login using the verification code sent to the client",
            Tags = ["ClientAuth"])]
        public async Task<ActionResult> VerifyAndLoginWithVerificationCodeAsync(
            VerifyAndLoginWithVerificationCodeUpdate update)
            => await _clientService.VerifyAndLoginWithVerificationCodeAsync(update);


        [HttpGet("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(
            Summary = "Renew client access token",
            Tags = ["ClientAuth"])]
        public async Task<ActionResult> RenewTokenAsync()
            => await _clientService.RenewTokenAsync(
                JwtToken.ToString(),
                PublicKey,
                Role);

        #endregion


        #region Profile

        [HttpGet("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(
            Summary = "Get authenticated client profile",
            Tags = ["ClientProfile"])]
        public async Task<ClientResult> GetClientDataAsync()
            => await _clientService.GetClientDataAsync(PublicKey);


        [HttpPut("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(
            Summary = "Create or update client profile data",
            Tags = ["ClientProfile"])]
        public async Task<ClientResult> UpsertProfileDataAsync(
            ClientProfileDataUpdate update)
            => await _clientService.UpsertProfileDataAsync(update, PublicKey);


        [HttpPut("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(
            Summary = "Request to change client phone number",
            Tags = ["ClientProfile"])]
        public async Task<bool> RequestChangePhoneNumberAsync(
            ChangePhoneNumberUpdate update)
            => await _clientService.RequestChangePhoneNumberAsync(update, PublicKey);


        [HttpPut("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(
            Summary = "Verify client phone number change request",
            Tags = ["ClientProfile"])]
        public async Task<bool> VerifyChangePhoneNumberAsync(
            VerifyChangePhoneNumberUpdate update)
            => await _clientService.VerifyChangePhoneNumberAsync(update, PublicKey);

        #endregion
    }
}
