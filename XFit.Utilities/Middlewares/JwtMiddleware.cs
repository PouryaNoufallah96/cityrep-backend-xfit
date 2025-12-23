using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using XFit.Utilities.Constants;
using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions;
using XFit.Utilities.Services.Contracts;
using XFit.Utilities.Utilities;

namespace XFit.Utilities.Middlewares
{
    public class JwtMiddleware(RequestDelegate next, IJwtService jwtService)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                JwtSecurityToken jwtToken;


                try
                {
                    jwtToken = jwtService.Validate(token);
                }
                catch (Exception ex)
                {
                    throw new AuthorizationException($"Invalid access token: {ex.Message}");
                    //await context.WriteToResponseAsync($"Invalid access token: {ex.Message}", HttpStatusCode.Unauthorized, ApiResultStatusCode.UnAuthorized);
                    //return;
                }


                context.Items["Token"] = jwtToken;

                var publicKey = jwtToken?.Claims.FirstOrDefault(c => c.Type == Claims.PublicKey.ToDisplay())?.Value ?? "system";
                var role = jwtToken?.Claims.FirstOrDefault(c => c.Type == Claims.Role.ToDisplay())?.Value ?? "system";
                var type = jwtToken?.Claims.FirstOrDefault(c => c.Type == Claims.UserType.ToDisplay())?.Value ?? "system";
                var phone = jwtToken?.Claims.FirstOrDefault(c => c.Type == Claims.PhoneNumber.ToDisplay())?.Value ?? "system";
                var permissions = jwtToken?.Claims.Where(c => c.Type == Claims.Permission.ToDisplay())?.Select(c => c.Value).ToList() ?? [];

                CurrentRequestContext.User = new RequestUserInfo
                {
                    PublicKey = publicKey,
                    Role = role,
                    Type = type,
                    Permissions = permissions,
                    PhoneNumber = phone
                };
            }

            await next(context);
        }
    }
}
