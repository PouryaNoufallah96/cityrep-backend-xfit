using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions.Common;

namespace XFit.Utilities.Exceptions
{
    public class AuthorizationException : BaseException
    {
        public AuthorizationException()
           : base(ApiResultStatusCode.UnAuthorized)
        {
        }
        public AuthorizationException(string message)
            : base(ApiResultStatusCode.UnAuthorized, System.Net.HttpStatusCode.Unauthorized, message)
        {
        }
    }
}
