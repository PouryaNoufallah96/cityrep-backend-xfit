using Microsoft.AspNetCore.Mvc.Filters;
using XFit.Utilities.Exceptions;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.Enums;
using XFit.Utilities.Extension;
using XFit.Utilities.Utilities;

namespace XFit.Utilities.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _claims;
        public bool RequireActiveUser { get; set; } = true;
        public AuthorizeAttribute()
        {
        }

        public AuthorizeAttribute(params string[] claims)
        {
            _claims = claims;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var jwtSecurityToken = context.HttpContext.GetToken();

            if (jwtSecurityToken == null)
                throw new AuthorizationException("Authorization error");

            if (_claims != null && !_claims.Any(c => jwtSecurityToken.HasClaim(Claims.Permission.ToDisplay(), c)))
                throw new BaseException(ApiResultStatusCode.Forbidden, "Access denied");

            if (RequireActiveUser)
            {
                var statusClaim = jwtSecurityToken.Claims
                    .FirstOrDefault(c => c.Type == Claims.UserStatus.ToDisplay());

                if (statusClaim == null || statusClaim.Value != "Active")
                    throw new BaseException(ApiResultStatusCode.Forbidden, "Please use the RZ Prime web application to submit new orders, apply changes or complete transactions.");
                    //throw new BaseException(ApiResultStatusCode.Forbidden, "Please connect your wallet in Settings to submit new orders, apply changes or complete transactions.");
            }

        }
    }
}
