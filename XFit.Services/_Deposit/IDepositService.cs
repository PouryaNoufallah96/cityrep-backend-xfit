using Xfit.Domain.Collections;
using XFit.Services._Deposit.DTOs;
using XFit.Utilities.MongoDatabase.Filter;

namespace XFit.Services._Deposit
{
    public interface IDepositService
    {
        Task<string> CreateDepositAsync(CreateDepositUpdate update,string whois);
        Task<string> CreateBookingDepositAsync(decimal amount, string whois);

        Task<DepositResult> VerifyDepositAsync(VerifyDepositUpdate update);
        Task ProcessForPendingDepositsAsync();

        Task<MonjoFilteredResult<Deposit>> GetAllDepositsAsync(MonjoQuery monjoQuery);

    }
}
