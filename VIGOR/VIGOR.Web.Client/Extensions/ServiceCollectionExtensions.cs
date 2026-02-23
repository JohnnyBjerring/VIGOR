using VIGOR.Shared.Interfaces.Services;
using VIGOR.Web.Client.Services;

namespace VIGOR.Web.Client.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVigorClientServices(this IServiceCollection services)
        {
            // Device / platform info
            services.AddSingleton<IFormFactor, FormFactor>();

            // TODO: Add HttpClient, API clients, AuthenticationStateProvider, etc.
            // services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001") });

            return services;
        }
    }
}
