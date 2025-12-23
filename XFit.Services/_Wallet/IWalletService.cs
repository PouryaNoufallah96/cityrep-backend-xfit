using Xfit.Domain.Common;
using XFit.Services._Wallet.DTOs;

namespace XFit.Services._Wallet
{
    public interface IWalletService
    {
        Task<WalletResult> GetOrCreateWalletAsync(string whois , string userRole);
        Task MakeWalletShouldUpdateAsync(string publicKey);
        Task MakeWalletShouldUpdateAsync(List<string> publicKeys);
        Task InitWalletAsync(string publicKey, UserRole userRole);
        Task SyncWalletAsync();
    }
}
