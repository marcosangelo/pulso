using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using Pulso.Hardware;

namespace Pulso.Link;

public sealed class CompanionHub : IDisposable
{
    public const int Port = PairingUri.DefaultPort;

    private readonly HttpListener _listener = new();
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private readonly object _jsonGate = new();
    private CancellationTokenSource? _cts;
    private string _json = """{"v":1}""";
    private string _token = PairingUri.NewToken();

    public string Token => _token;
    public int ClientCount => _clients.Count;
    public event Action? Changed;

    public bool Start(IEnumerable<string> hosts)
    {
        Stop();
        _cts = new CancellationTokenSource();
        try
        {
            _listener.Prefixes.Clear();
            foreach (var host in hosts.Distinct())
                _listener.Prefixes.Add($"http://{host}:{Port}/");
            if (_listener.Prefixes.Count == 0)
                _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
        }
        catch
        {
            return false;
        }
        _ = Task.Run(() => AcceptLoop(_cts.Token));
        Changed?.Invoke();
        return true;
    }

    public void RotateToken()
    {
        _token = PairingUri.NewToken();
        foreach (var kv in _clients)
        {
            _clients.TryRemove(kv.Key, out _);
            try { kv.Value.Abort(); } catch { /* ignore */ }
        }
        Changed?.Invoke();
    }

    public void Publish(HardwareSample sample)
    {
        var json = TelemetryEnvelope.From(sample).ToJson();
        lock (_jsonGate) _json = json;
        var bytes = Encoding.UTF8.GetBytes(json);
        foreach (var kv in _clients)
        {
            var ws = kv.Value;
            if (ws.State != WebSocketState.Open)
            {
                _clients.TryRemove(kv.Key, out _);
                continue;
            }
            try
            {
                _ = ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch
            {
                _clients.TryRemove(kv.Key, out _);
            }
        }
    }

    private async Task AcceptLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync().ConfigureAwait(false); }
            catch { break; }
            _ = Task.Run(() => Handle(ctx, ct), ct);
        }
    }

    private async Task Handle(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath.TrimEnd('/') ?? "";
            if (path.Equals("/health", StringComparison.OrdinalIgnoreCase))
            {
                await Write(ctx, 200, """{"ok":true,"v":1}""");
                return;
            }
            if (path.Equals("/v1/snapshot", StringComparison.OrdinalIgnoreCase))
            {
                if (!TokenOk(ctx)) { await Write(ctx, 401, """{"err":"token"}"""); return; }
                string json;
                lock (_jsonGate) json = _json;
                await Write(ctx, 200, json);
                return;
            }
            if (path.Equals("/v1/live", StringComparison.OrdinalIgnoreCase) && ctx.Request.IsWebSocketRequest)
            {
                if (!TokenOk(ctx))
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.Close();
                    return;
                }
                var wsCtx = await ctx.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
                await Pump(wsCtx.WebSocket, ct).ConfigureAwait(false);
                return;
            }
            await Write(ctx, 404, """{"err":"not_found"}""");
        }
        catch
        {
            try { ctx.Response.Abort(); } catch { /* ignore */ }
        }
    }

    private bool TokenOk(HttpListenerContext ctx) =>
        string.Equals(ctx.Request.QueryString["t"], _token, StringComparison.Ordinal);

    private async Task Pump(WebSocket ws, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        _clients[id] = ws;
        Changed?.Invoke();
        try
        {
            string json;
            lock (_jsonGate) json = _json;
            await ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, ct);
            var buf = new byte[8];
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buf, ct).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close) break;
            }
        }
        catch
        {
            // socket caiu
        }
        finally
        {
            _clients.TryRemove(id, out _);
            try { ws.Dispose(); } catch { /* ignore */ }
            Changed?.Invoke();
        }
    }

    private static async Task Write(HttpListenerContext ctx, int status, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.Close();
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { if (_listener.IsListening) _listener.Stop(); } catch { /* ignore */ }
        foreach (var kv in _clients)
        {
            try { kv.Value.Abort(); } catch { /* ignore */ }
        }
        _clients.Clear();
    }

    public void Dispose()
    {
        Stop();
        try { _listener.Close(); } catch { /* ignore */ }
        _cts?.Dispose();
    }
}
