using XFit.Utilities.Models.Results;

namespace XFit.Utilities.Services.Contracts
{
    public interface ICaptchaService
    {
        Task ValidateCaptchaAsync(string captchaKey, string captchaCode);
        Task<GetCaptchaResult> GetCaptchaAsync();
    }
}
