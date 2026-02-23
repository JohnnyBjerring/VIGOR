using VIGOR.Shared.Interfaces.Services;
using VIGOR.Web.Services;

namespace VIGOR.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVigorWebServices(this IServiceCollection services)
        {
            // Device / platform info
            services.AddSingleton<IFormFactor, FormFactor>();

            // TODO: Add DbContext, Repositories, and other server-side services here
            // services.AddDbContext<AppDbContext>(options => ...);
            // services.AddScoped<IPlayerRepository, PlayerRepository>();

            return services;
        }
    }
}
