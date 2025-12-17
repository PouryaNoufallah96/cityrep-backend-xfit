using XFit.Utilities.Attributes;

namespace XFit.Services._Common.DTOs
{
    public class GetVerificationCodeForAuthenticationUpdate
    {
        [StringInputValidation(maxLength: 11, minLength: 11)] public required string PhoneNumber { get; set; }
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
    }



    public class VerifyAndLoginWithVerificationCodeUpdate
    {
        [StringInputValidation(minLength: 11, maxLength: 11)] public required string PhoneNumber { get; set; }
        [StringInputValidation(true, maxLength: 6, minLength: 6)] public required string VerificationCode { get; set; }

        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }

        public string CaptchaKey { get; set; }
        public string CaptchaCode { get; set; }
    }


    public class ChangePhoneNumberUpdate
    {
        [StringInputValidation(maxLength: 11, minLength: 11)] public required string PhoneNumber { get; set; }
    }

    public class VerifyChangePhoneNumberUpdate
    {
        [StringInputValidation(true, maxLength: 6, minLength: 6)] public required string VerificationCode { get; set; }

        public string CaptchaKey { get; set; }
        public string CaptchaCode { get; set; }
    }


    public record ResetPasswordUpdate
    {
        [GuidInputValidation] public required string PublicKey { get; set; }
        [StringInputValidation(true, maxLength: 100)] public required string Password { get; set; }
    }



}
