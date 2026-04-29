using DeltaX.SettingManager;
using Microsoft.Extensions.DependencyInjection;



namespace DeltaX.Infrastructure.Initialization
{
    public static class SettingManagerServiceExtensions
    {
        public static IServiceCollection AddSettingPage<T>(this IServiceCollection services)
            where T : SettingsPage
        {
            // Register the type T so the InitializableModule can find it later
            services.AddSingleton<SettingsRegistration>(new SettingsRegistration(typeof(T)));

            // Register the actual manager
            services.AddSingleton<SettingManager.SettingManager>();

            return services;
        }
    }
}