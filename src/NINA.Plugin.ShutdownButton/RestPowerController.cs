using System.Net.Http;
using System.Text;

namespace NINA.Plugin.ShutdownButton;

/// <summary>
/// Explicitly named power outlets. Only enabled entries are sent; no automatic
/// fallback to a whole-observatory REST endpoint when per-output mode is configured.
/// </summary>
public sealed class RestPowerController {
    private readonly ShutdownSettings settings;
    public RestPowerController(ShutdownSettings settings) => this.settings = settings;

    public async Task ExecuteAsync(CancellationToken ct) {
        if (settings.DryRun) return;
        if (!settings.EnableRestPowerOff) return;

        var selected = settings.PowerOutputs.Where(a => a.Enabled).ToArray();
        if (selected.Length == 0)
            throw new InvalidOperationException("REST enabled, but no individual power outputs are selected.");

        // Validate *all* actions before sending the first request.
        var requests = new List<(PowerOffAction Action, Uri Uri)>();
        foreach (var action in selected) {
            if (string.IsNullOrWhiteSpace(action.Name)
                || !Uri.TryCreate(action.Url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidOperationException("An enabled power output has missing name or invalid URL.");
            if (!new[] { "POST", "PUT", "PATCH" }.Contains(action.Method.ToUpperInvariant()))
                throw new InvalidOperationException($"Output '{action.Name}' uses an unsupported HTTP method.");
            requests.Add((action, uri));
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        foreach (var (action, uri) in requests) {
            ct.ThrowIfCancellationRequested();
            using var request = new HttpRequestMessage(new HttpMethod(action.Method), uri);
            if (!string.IsNullOrWhiteSpace(action.AuthorizationHeader)) {
                if (!request.Headers.TryAddWithoutValidation("Authorization", action.AuthorizationHeader))
                    throw new InvalidOperationException($"Invalid Authorization header for '{action.Name}'.");
            }
            if (action.Body is not null)
                request.Content = new StringContent(action.Body, Encoding.UTF8, "application/json");
            try {
                using var response = await http.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Output '{action.Name}' returned HTTP {(int)response.StatusCode}.");
            } catch (HttpRequestException) {
                // Never include URLs, request bodies or headers in diagnostics.
                throw new InvalidOperationException($"Connection to output '{action.Name}' failed.");
            }
        }
    }
}
