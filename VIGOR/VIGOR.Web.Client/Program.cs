using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VIGOR.Web.Client.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add VIGOR Client services (DI)
builder.Services.AddVigorClientServices();

await builder.Build().RunAsync();
