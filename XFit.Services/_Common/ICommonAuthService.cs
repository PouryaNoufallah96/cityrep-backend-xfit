using Microsoft.AspNetCore.Mvc;
using XFit.Services._Common.DTOs;

namespace XFit.Services._Common
{
    public interface ICommonAuthService
    {
        Task<bool> GetVerificationCodeForAuthenticationAsync(GetVerificationCodeForAuthenticationUpdate update);
        Task<ActionResult> VerifyAndLoginWithVerificationCodeAsync(VerifyAndLoginWithVerificationCodeUpdate update);
        Task<bool> RequestChangePhoneNumberAsync(ChangePhoneNumberUpdate update, string whois);
        Task<bool> VerifyChangePhoneNumberAsync(VerifyChangePhoneNumberUpdate update, string whois);
        Task<ActionResult> RenewTokenAsync(string token, string whoIs, string role);

    }
}
