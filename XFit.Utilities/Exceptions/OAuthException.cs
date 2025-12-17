using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions.Common;

namespace XFit.Utilities.Exceptions
{
    public class OAuthException : BaseException
    {
        public OAuthException()
           : base(ApiResultStatusCode.OAuth)
        {
        }
        public OAuthException(string message)
            : base(ApiResultStatusCode.OAuth, message)
        {
        }
    }
}
