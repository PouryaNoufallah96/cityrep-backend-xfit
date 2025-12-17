using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions.Common;

namespace XFit.Utilities.Exceptions
{
    public class DuplicateException : BaseException
    {
        public DuplicateException()
           : base(ApiResultStatusCode.Duplicated)
        {
        }
        public DuplicateException(string message)
            : base(ApiResultStatusCode.Duplicated, message)
        {
        }
    }
}
