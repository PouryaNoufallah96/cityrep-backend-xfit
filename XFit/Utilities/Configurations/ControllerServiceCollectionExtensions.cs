using Microsoft.Extensions.Options;
using XFit.Services._Deposit.DTOs.Settings;
using XFit.Services._File.DTOs.Settings;
using XFit.Services._Gateway;
using XFit.Services._Gym.DTOs.Settings;

namespace XFit.Utilities.Configurations
{
    public static class ControllerServiceCollectionExtensions
    {
        public static void AddSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterSetting<GymLevelSettings>(configuration.GetSection(nameof(GymLevelSettings)));
            services.RegisterSetting<IRTHandlerSettings>(configuration.GetSection(nameof(IRTHandlerSettings)));
            services.RegisterSetting<FileSettings>(configuration.GetSection(nameof(FileSettings)));
            services.RegisterSetting<MockPaymentSettings>(configuration.GetSection(nameof(MockPaymentSettings)));
        }

        private static void RegisterSetting<TSettings>(this IServiceCollection services, IConfigurationSection configuration)
           where TSettings : class, new()
        {
            services.Configure<TSettings>(configuration);
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<TSettings>>().Value);
        }

        private static void RegisterSetting<TSettings, TISettings>(this IServiceCollection services, IConfigurationSection configuration)
            where TISettings : class
            where TSettings : class, TISettings, new()
        {
            services.Configure<TSettings>(configuration);
            services.AddSingleton<TISettings>(sp => sp.GetRequiredService<IOptions<TSettings>>().Value);
        }
    }
}
