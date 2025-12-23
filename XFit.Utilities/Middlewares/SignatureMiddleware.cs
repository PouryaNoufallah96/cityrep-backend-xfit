using Microsoft.AspNetCore.Http;
using System.Text;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.Services.Contracts;
using XFit.Utilities.Models.Settings;
using XFit.Utilities.Utilities;


namespace XFit.Utilities.Middlewares
{
    public class SignatureMiddleware(RequestDelegate _next, ISignatureService _signatureService,
        INonceService _nonceService, ApplicationPoolSettings _applicationPool)
    {
        public async Task InvokeAsync(HttpContext context)
        {

            if (
                context.Request.Path.StartsWithSegments("/api/v1/File/DownloadFile"))
            {
                await _next(context);
                return;
            }

            var applicationId = context.Request.Headers["ApplicationId"].FirstOrDefault();
            var nonce = context.Request.Headers["Nonce"].FirstOrDefault();
            var signature = context.Request.Headers["Signature"].FirstOrDefault();

            var application = _applicationPool.Applications.FirstOrDefault(q => q.ApplicationId == applicationId)
                .CheckNotNull("Application not found");

            if (string.IsNullOrEmpty(nonce) && signature != application.MasterSignature)
                throw new BadRequestException("Nonce is not specified");

            if (string.IsNullOrEmpty(signature))
                throw new BadRequestException("Signature is not specified");

            if (_nonceService.Contains(nonce) && signature != application.MasterSignature)
                throw new BadRequestException("Invalid nonce!");

            if (signature != application.MasterSignature)
            {
                byte[] signatureBytes = Convert.FromBase64String(signature);

                bool verification = _signatureService.Verify(Encoding.UTF8.GetBytes(application.PreSharedKey),
                                   Encoding.UTF8.GetBytes(nonce ?? string.Empty), signatureBytes);

                if (!verification)
                    throw new BadRequestException("Signature is not valid");

                _nonceService.Add(nonce);
            }

            await _next(context);
        }
    }
}