using VIGOR.Services;
using VIGOR.Shared.Interfaces.Services;

namespace VIGOR.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVigorMauiServices(this IServiceCollection services)
        {
            // Device / platform info
            services.AddSingleton<IFormFactor, FormFactor>();

            // TODO: Add MAUI-specific services here
            // services.AddSingleton<ISecureStorage, SecureStorageService>();

            return services;
        }
    }
}
