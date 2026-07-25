using System.Net;
using XFit.Utilities.Enums;

namespace XFit.Utilities.Exceptions.Common
{
    public class NotFoundException : BaseException
    {
        public NotFoundException()
            : base(ApiResultStatusCode.NotFound, HttpStatusCode.NotFound, null)
        {
        }

        public NotFoundException(string message)
            : base(ApiResultStatusCode.NotFound, HttpStatusCode.NotFound, message)
        {
        }


        public NotFoundException(ApiResultStatusCode status, string message)
          : base(HttpStatusCode.NotFound, status, message)
        {
        }

        public NotFoundException(object additionalData, string message)
            : base(ApiResultStatusCode.NotFound, message, HttpStatusCode.NotFound, additionalData)
        {
        }

        public NotFoundException(string message, object additionalData)
            : base(ApiResultStatusCode.NotFound, message, HttpStatusCode.NotFound, additionalData)
        {
        }

        public NotFoundException(string message, Exception exception)
            : base(ApiResultStatusCode.NotFound, message, HttpStatusCode.NotFound, exception)
        {
        }

        public NotFoundException(string message, Exception exception, object additionalData)
            : base(ApiResultStatusCode.NotFound, message, HttpStatusCode.NotFound, exception, additionalData)
        {
        }
    }
}
