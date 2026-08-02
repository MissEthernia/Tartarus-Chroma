using System.Net.Http.Json;
using System.Text.Json;

namespace TartarusChroma;

internal sealed class ChromaRestClient : IAsyncDisposable
{
    private static readonly Uri RegistrationUri =
        new("http://localhost:54235/razer/chromasdk");

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    private CancellationTokenSource? _heartbeatCts;
    private Task? _heartbeatTask;

    public Uri? SessionUri { get; private set; }
    public event Action<string>? Log;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (SessionUri is not null)
            return;

        var registration = new
        {
            title = "Tartarus Chroma",
            description = "Shows active macro states on Razer Chroma devices.",
            author = new
            {
                name = "Antonia Weiss",
                contact = "https://github.com/MissEthernia/Tartarus-Chroma"
            },
            device_supported = new[] { "keypad", "keyboard" },
            category = "application"
        };

        LogMessage($"POST {RegistrationUri}");
        using HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(RegistrationUri, registration, cancellationToken);

        string raw = await response.Content.ReadAsStringAsync(cancellationToken);
        LogMessage($"HTTP {(int)response.StatusCode}: {raw}");
        response.EnsureSuccessStatusCode();

        using JsonDocument json = JsonDocument.Parse(raw);
        if (!json.RootElement.TryGetProperty("uri", out JsonElement uriElement) ||
            !Uri.TryCreate(uriElement.GetString(), UriKind.Absolute, out Uri? sessionUri))
        {
            throw new InvalidOperationException(
                "Razer Chroma hat keine gültige Sitzungsadresse geliefert.");
        }

        SessionUri = sessionUri;
        StartHeartbeat();
        LogMessage($"Verbunden: {SessionUri}");
    }

    public async Task SetKeypadColorsAsync(
        IReadOnlyList<int> colors,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        if (colors.Count != 20)
            throw new ArgumentException("Das Tartarus-Farbraster benötigt genau 20 Werte.");

        int[][] matrix =
        [
            colors.Take(5).ToArray(),
            colors.Skip(5).Take(5).ToArray(),
            colors.Skip(10).Take(5).ToArray(),
            colors.Skip(15).Take(5).ToArray()
        ];

        await PutEffectAsync("keypad", new
        {
            effect = "CHROMA_CUSTOM",
            param = matrix
        }, cancellationToken);
    }

    public async Task SetKeypadStaticAsync(
        int color,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await PutEffectAsync("keypad", new
        {
            effect = "CHROMA_STATIC",
            param = new { color }
        }, cancellationToken);
    }

    public async Task SetKeyboardStaticAsync(
        int color,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await PutEffectAsync("keyboard", new
        {
            effect = "CHROMA_STATIC",
            param = new { color }
        }, cancellationToken);
    }

    public async Task ReleaseAsync(CancellationToken cancellationToken = default)
    {
        if (SessionUri is null)
            return;

        _heartbeatCts?.Cancel();

        try
        {
            using HttpResponseMessage response =
                await _httpClient.DeleteAsync(SessionUri, cancellationToken);
            LogMessage($"DELETE {SessionUri}: HTTP {(int)response.StatusCode}");
        }
        finally
        {
            SessionUri = null;
        }
    }

    private async Task PutEffectAsync(
        string device,
        object payload,
        CancellationToken cancellationToken)
    {
        Uri endpoint = new(SessionUri!, device);
        LogMessage($"PUT {endpoint}");

        using HttpResponseMessage response =
            await _httpClient.PutAsJsonAsync(endpoint, payload, cancellationToken);

        string raw = await response.Content.ReadAsStringAsync(cancellationToken);
        LogMessage($"HTTP {(int)response.StatusCode}: {raw}");
        response.EnsureSuccessStatusCode();

        using JsonDocument json = JsonDocument.Parse(raw);
        if (json.RootElement.TryGetProperty("result", out JsonElement resultElement) &&
            resultElement.GetInt32() != 0)
        {
            throw new InvalidOperationException(
                $"Razer Chroma meldet Fehlercode {resultElement.GetInt32()}.");
        }
    }

    private void StartHeartbeat()
    {
        _heartbeatCts = new CancellationTokenSource();
        CancellationToken token = _heartbeatCts.Token;

        _heartbeatTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && SessionUri is not null)
            {
                try
                {
                    Uri endpoint = new(SessionUri, "heartbeat");
                    using HttpResponseMessage response =
                        await _httpClient.PutAsJsonAsync(endpoint, new { }, token);
                    LogMessage($"Heartbeat: HTTP {(int)response.StatusCode}");
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogMessage($"Heartbeat-Fehler: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void EnsureConnected()
    {
        if (SessionUri is null)
            throw new InvalidOperationException("Noch keine Verbindung zum Chroma SDK.");
    }

    private void LogMessage(string message) =>
        Log?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");

    public async ValueTask DisposeAsync()
    {
        try
        {
            await ReleaseAsync();
        }
        catch
        {
            // Anwendung wird beendet; Freigabefehler nicht erneut werfen.
        }

        _heartbeatCts?.Dispose();
        _httpClient.Dispose();
    }

    public static int ToBgr(System.Drawing.Color color) =>
        color.R | (color.G << 8) | (color.B << 16);
}
