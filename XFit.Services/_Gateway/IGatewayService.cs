using XFit.Services._Gateway.DTOs;

namespace XFit.Services._Gateway
{
    public interface IGatewayService
    {
        Task<string> CreateZarinPalDepositAsync(string depositId, CreateIRTDepositUpdate update);
        Task<VerifyIRTDepositResult> VerifyZarinPalDepositAsync(string trackId, decimal amount);
    }
}
 