using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfit.Domain.Common;
using XFit.Services._Wallet.DTOs;

namespace XFit.Services._Wallet
{
    public interface IWalletService
    {
        Task<WalletResult> GetOrCreateWalletAsync(string whois , string userRole);
    }
}
