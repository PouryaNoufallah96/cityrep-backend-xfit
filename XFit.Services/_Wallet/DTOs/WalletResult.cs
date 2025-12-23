using Xfit.Domain.Collections;

namespace XFit.Services._Wallet.DTOs
{
    public class WalletResult
    {
        public string WalletId { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal FrozenBalance { get; set; }
    }

    public class WithdrawalSumResult
    {
        public WithdrawalState State { get; set; }
        public decimal TotalAmount { get; set; }
    }


}
