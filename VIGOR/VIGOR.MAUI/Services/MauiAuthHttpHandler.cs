using System.Net.Http.Headers;

namespace VIGOR.MAUI.Services;

public class MauiAuthHttpHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync("jwt_token");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}