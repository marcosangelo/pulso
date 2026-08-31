using System.Net.WebSockets;
using System.Text;

namespace Pulso.Link;

/// <summary>
/// Publica o mesmo JSON do hub num relay (DigitalOcean). O PC inicia a conexão de saída —
/// sem abrir porta em casa. URL tipo ws://IP:8080 ou wss://dominio.
/// </summary>
public sealed class RelayClient : IDisposable
{
    private readonly object _gate = new();
    private string? _base;
    private string _token = "";
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private string _last = """{"v":1}""";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_base);
    public string? LastError { get; private set; }

    public void Configure(string? baseUrl, string token)
    {
        lock (_gate)
        {
            _token = token;
            var next = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim().TrimEnd('/');
            if (next == _base && _ws?.State == WebSocketState.Open) return;
            _base = next;
        }
        _ = RunLoop();
    }

    public void Publish(string json)
    {
        lock (_gate) _last = json;
        var ws = _ws;
        if (ws is not { State: WebSocketState.Open }) return;
        var bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            _ = ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            CompanionLog.Line($"relay send FAIL {ex.Message}");
        }
    }

    private async Task RunLoop()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        while (!ct.IsCancellationRequested)
        {
            string? baseUrl;
            string token;
            lock (_gate)
            {
                baseUrl = _base;
                token = _token;
            }
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                try { await Task.Delay(2000, ct); } catch { break; }
                continue;
            }

            var uri = BuildUp(baseUrl, token);
            var ws = new ClientWebSocket();
            try
            {
                CompanionLog.Line($"relay connect {uri.Host}:{uri.Port}");
                await ws.ConnectAsync(uri, ct).ConfigureAwait(false);
                _ws = ws;
                LastError = null;
                CompanionLog.Line("relay up");
                string snap;
                lock (_gate) snap = _last;
                await ws.SendAsync(Encoding.UTF8.GetBytes(snap), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
                var buf = new byte[8];
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var r = await ws.ReceiveAsync(buf, ct).ConfigureAwait(false);
                    if (r.MessageType == WebSocketMessageType.Close) break;
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                LastError = ex.Message;
                CompanionLog.Line($"relay FAIL {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                if (ReferenceEquals(_ws, ws)) _ws = null;
                try { ws.Dispose(); } catch { /* ignore */ }
            }
            try { await Task.Delay(4000, ct); } catch { break; }
        }
    }

    public static Uri? TryParseBase(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var text = raw.Trim();
        if (!text.Contains("://", StringComparison.Ordinal))
            text = "ws://" + text;
        return Uri.TryCreate(text, UriKind.Absolute, out var u)
               && u.Scheme is "ws" or "wss"
            ? u
            : null;
    }

    private static Uri BuildUp(string baseUrl, string token)
    {
        var u = TryParseBase(baseUrl) ?? throw new InvalidOperationException("relay URL");
        var b = new UriBuilder(u) { Path = "/v1/up", Query = "t=" + Uri.EscapeDataString(token) };
        return b.Uri;
    }

    public static (string Host, int Port, bool Secure)? QrTarget(string? raw)
    {
        var u = TryParseBase(raw);
        if (u is null) return null;
        var port = u.IsDefaultPort ? (u.Scheme == "wss" ? 443 : 80) : u.Port;
        return (u.Host, port, u.Scheme == "wss");
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _ws?.Abort(); } catch { /* ignore */ }
        _cts?.Dispose();
    }
}
