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


        }
    }
}
