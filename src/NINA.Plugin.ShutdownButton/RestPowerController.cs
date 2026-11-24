using System.Net.Http;

namespace NINA.Plugin.ShutdownButton;

public sealed class RestPowerController {
    private readonly ShutdownSettings settings;
    public RestPowerController(ShutdownSettings settings) => this.settings = settings;

    public async Task ExecuteAsync(CancellationToken ct) {
        // Dry-run and unconfigured cases MUST NOT issue HTTP requests.
        if (settings.DryRun) return;
        if (!settings.EnableRestPowerOff)
            throw new InvalidOperationException("REST power-off disabled. Enable it explicitly after dry-run tests.");
        if (!Uri.TryCreate(settings.RestUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("REST URL not configured or invalid.");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        using var request = new HttpRequestMessage(new HttpMethod(settings.RestMethod), uri);
        if (!string.IsNullOrWhiteSpace(settings.AuthorizationHeader))
            request.Headers.TryAddWithoutValidation("Authorization", settings.AuthorizationHeader);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Power controller returned HTTP {(int)response.StatusCode}.");
    }
}
