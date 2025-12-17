using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using XFit.Utilities.Enums;
using XFit.Utilities.Extension;
using XFit.Utilities.Utilities;

namespace XFit.Utilities.Api
{
    public class ApiBaseController : ControllerBase
    {
        protected virtual JwtSecurityToken JwtToken => (JwtSecurityToken)HttpContext.Items["Token"];
        protected virtual string PublicKey => HttpContext.GetClaim(Claims.PublicKey.ToDisplay());
        protected virtual string Role => HttpContext.GetClaim(Claims.Role.ToDisplay()); 
        protected virtual string UserStatus => HttpContext.GetClaim(Claims.UserStatus.ToDisplay()); 
        protected virtual string Language => HttpContext.Request.Headers["Accept-Language"];
        protected virtual string Nonce => HttpContext.Request.Headers["DecryptedNonce"].ToString();
        protected virtual string Ip => HttpContext.GetRequestIpv4();

    }
}
