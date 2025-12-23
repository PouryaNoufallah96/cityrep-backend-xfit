using Microsoft.Extensions.DependencyInjection;
using XFit.Utilities.Services;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Wallet.BackgroundServices
{
    public class SyncWalletScheduler(IServiceProvider serviceProvider)
    : SchedulerBase(serviceProvider, TimeSpan.FromSeconds(1)), IHostedDependency
    {
        protected override async Task HandleAsync(IServiceProvider scopedProvider)
        {
            var walletService = scopedProvider.GetRequiredService<IWalletService>();

            await walletService.SyncWalletAsync();
        }
    }
}
