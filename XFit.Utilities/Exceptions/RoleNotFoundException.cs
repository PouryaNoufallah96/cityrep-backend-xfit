using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions.Common;

namespace XFit.Utilities.Exceptions
{
    public class RoleNotFoundException : BaseException
    {
        public RoleNotFoundException()
            : base(ApiResultStatusCode.RoleNotFound)
        {
        }
        public RoleNotFoundException(string message)
            : base(ApiResultStatusCode.RoleNotFound, System.Net.HttpStatusCode.NotFound, message)
        {
        }

    }
}
