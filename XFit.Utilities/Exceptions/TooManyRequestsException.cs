using System.Net;
using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions.Common;

namespace XFit.Utilities.Exceptions
{
    public class TooManyRequestsException : BaseException
    {
        public TooManyRequestsException()
           : base(ApiResultStatusCode.TooManyRequests)
        {
        }

        public TooManyRequestsException(string message)
            : base(ApiResultStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests, message)
        {
        }
    }
}
