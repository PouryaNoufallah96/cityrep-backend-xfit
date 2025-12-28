using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using XFit.Services._Wallet;
using XFit.Services._Wallet.DTOs;
using XFit.Utilities.Api;
using XFit.Utilities.Attributes;
using XFit.Utilities.DTOs;
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
        [CustomRateLimit]
        [Authorize]
        public async Task<WalletResult> GetOrCreateWalletAsync()
            => await _walletService.GetOrCreateWalletAsync(PublicKey, Role);


        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "get client transaction history for wallet page", Tags = ["C-TransactionHistory"])]
        public async Task<ClientTransactionListResult> GetClientTransactionsAsync(Pagination pagination)
            => await _walletService.GetClientTransactionsAsync(PublicKey, pagination);


        [HttpPost("[action]")]
        [CustomRateLimit]
        [Authorize]
        [SwaggerOperation(Summary = "get gym owner transaction history for wallet page", Tags = ["GO-TransactionHistory"])]
        public async Task<GymOwnerTransactionListResult> GetGymOwnerTransactionsAsync(Pagination pagination)
            => await _walletService.GetGymOwnerTransactionsAsync(PublicKey, pagination);




    }
}
