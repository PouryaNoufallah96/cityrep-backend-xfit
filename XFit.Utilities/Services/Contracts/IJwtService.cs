using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using XFit.Utilities.Enums;

namespace XFit.Utilities.Services.Contracts
{
    public interface IJwtService
    {
        AccessToken Generate(IEnumerable<Claim> claims);
        JwtSecurityToken Validate(string token);
        ActionResult Authenticate(string publicKey, string userRole, string phoneNumber, IEnumerable<string> permissions,
             string securityStamp);

    }
}