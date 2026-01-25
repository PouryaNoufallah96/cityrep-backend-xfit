using Microsoft.Extensions.DependencyInjection;
using XFit.Utilities.Services;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._GymAttendance.BackGroundServices
{
    public class ExpireAttendanceScheduler(IServiceProvider serviceProvider)
        : SchedulerBase(serviceProvider, TimeSpan.FromMinutes(5)), IHostedDependency
    {
        protected override async Task HandleAsync(IServiceProvider scopedProvider)
        {
            var gymAttendance = scopedProvider.GetRequiredService<IGymAttendanceService>();

            await gymAttendance.ExpireAttendanceAsync();
        }
    }
}
