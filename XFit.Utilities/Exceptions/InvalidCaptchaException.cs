using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions.Common;

namespace XFit.Utilities.Exceptions
{
    public class InvalidCaptchaException : BaseException
    {
        public InvalidCaptchaException()
           : base(ApiResultStatusCode.InvalidCaptcha)
        {
        }
        public InvalidCaptchaException(string message)
            : base(ApiResultStatusCode.InvalidCaptcha, message)
        {
        }

    }
}
