using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions.Common;

namespace XFit.Utilities.Exceptions
{
    public class ExpireVerificationException : BaseException
    {
        public ExpireVerificationException()
           : base(ApiResultStatusCode.ExpireVerification)
        {
        }
        public ExpireVerificationException(string message)
            : base(ApiResultStatusCode.ExpireVerification, System.Net.HttpStatusCode.BadRequest, message)
        {
        }
    }
}
