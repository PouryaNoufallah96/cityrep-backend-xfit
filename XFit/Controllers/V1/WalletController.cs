using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using XFit.Services._Wallet;
using XFit.Services._Wallet.DTOs;
using XFit.Utilities.Api;
using XFit.Utilities.Filters;

namespace XFit.Controllers.V1
{

    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class WalletController(IWalletService _walletService) : ApiBaseController
    {

        [HttpGet("[action]")]
        [Authorize]
        public async Task<WalletResult> GetOrCreateWalletAsync()
            => await _walletService.GetOrCreateWalletAsync(PublicKey, Role);
    }
}
