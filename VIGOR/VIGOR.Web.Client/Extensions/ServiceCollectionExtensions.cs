using System;
using System.Net.Http;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Shared.Services;
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
            // Minimal HttpClient for calling the Web API used in iteration 2.
            // Note: For development the API runs on http://localhost:5249/ (same as MAUI project).
            services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5249/") });

            // Minimal client-side implementation of ICitizenService that calls the existing API
            services.AddScoped<ICitizenService, CitizenClientService>();

            return services;
        }
    }
}
