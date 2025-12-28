using Xfit.Domain.Common;
using XFit.Services._Wallet.DTOs;
using XFit.Utilities.DTOs;

namespace XFit.Services._Wallet
{
    public interface IWalletService
    {
        Task<WalletResult> GetOrCreateWalletAsync(string whois , string userRole);
        Task MakeWalletShouldUpdateAsync(string publicKey);
        Task MakeWalletShouldUpdateAsync(List<string> publicKeys);
        Task InitWalletAsync(string publicKey, UserRole userRole);
        Task SyncWalletAsync();

        Task<ClientTransactionListResult> GetClientTransactionsAsync(string publicKey,Pagination pagination);
        Task<GymOwnerTransactionListResult> GetGymOwnerTransactionsAsync(string publicKey,Pagination pagination);
    }
}
