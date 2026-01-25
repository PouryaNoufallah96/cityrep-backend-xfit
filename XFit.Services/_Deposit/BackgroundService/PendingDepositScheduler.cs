using Microsoft.Extensions.DependencyInjection;
using XFit.Utilities.Services;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Deposit.BackgroundService
{
    public class PendingDepositScheduler(IServiceProvider serviceProvider) : SchedulerBase(serviceProvider, TimeSpan.FromMinutes(1)), IHostedDependency
    {
        protected override async Task HandleAsync(IServiceProvider scopedProvider)
        {
            var depositService = scopedProvider.GetRequiredService<IDepositService>();
            await depositService.ProcessForPendingDepositsAsync();
        }
    }
}
