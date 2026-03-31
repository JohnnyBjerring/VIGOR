using System.Net.Http.Headers;

namespace VIGOR.MAUI.Services;

public class MauiAuthHttpHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync("jwt_token");
        System.Diagnostics.Debug.WriteLine($"MauiAuthHttpHandler running for {request.RequestUri}. Token present: {!string.IsNullOrWhiteSpace(token)}");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else 
        {
            System.Diagnostics.Debug.WriteLine("MauiAuthHttpHandler: NO TOKEN FOUND in SecureStorage!");
        }
        return await base.SendAsync(request, cancellationToken);
    }
}