using Xfit.Domain.Collections;

namespace XFit.Services._Deposit.DTOs
{
    public class DepositResult
    {
        public decimal Amount { get; set; }
        public string Reference { get; set; }
        public DepositState State { get; set; }
    }
}
