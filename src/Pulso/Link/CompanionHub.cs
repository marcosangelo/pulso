using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Pulso.Hardware;

namespace Pulso.Link;

/// <summary>
/// Hub LAN na 8742. TcpListener em 0.0.0.0 — sem http.sys, então não precisa de URL ACL
/// e o firewall pergunta pelo Pulso.exe (HttpListener antigo só falava com localhost).
/// </summary>
public sealed class CompanionHub : IDisposable
{
    public const int Port = PairingUri.DefaultPort;
    private const string WsMagic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private readonly object _jsonGate = new();
    private TcpListener? _tcp;
    private CancellationTokenSource? _cts;
    private string _json = """{"v":1}""";
    private string _token = PairingUri.NewToken();

    public CompanionHub()
    {
        var saved = Theme.ThemeSettings.Load().PairToken;
        if (!string.IsNullOrWhiteSpace(saved) && saved.Length >= 8)
            _token = saved;
        else
            Theme.ThemeSettings.Update(s => s.PairToken = _token);
    }

    public string Token => _token;
    public int ClientCount => _clients.Count;
    public string? LastError { get; private set; }
    public bool IsListening => _tcp is not null && _cts is { IsCancellationRequested: false };
    public event Action? Changed;

    public bool Start(IEnumerable<string> hosts)
    {
        _ = hosts;
        if (IsListening) return true;
        Stop();
        LastError = null;
        _cts = new CancellationTokenSource();
        try
        {
            _tcp = new TcpListener(IPAddress.Any, Port);
            _tcp.Start();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            LastError = "Porta 8742 ocupada. Feche o outro Pulso.";
            CompanionLog.Line($"start FAIL port busy: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            CompanionLog.Line($"start FAIL {ex}");
            return false;
        }

        Firewall.TryAllowInbound(Port);
        CompanionLog.Line($"listen 0.0.0.0:{Port} token={_token[..4]}… admin={Privileges.IsAdministrator()}");
        _ = AcceptLoop(_cts.Token);
        Changed?.Invoke();
        return true;
    }

    public void RotateToken()
    {
        _token = PairingUri.NewToken();
        Theme.ThemeSettings.Update(s => s.PairToken = _token);
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
        while (!ct.IsCancellationRequested && _tcp is not null)
        {
            TcpClient client;
            try { client = await _tcp.AcceptTcpClientAsync(ct).ConfigureAwait(false); }
            catch (Exception ex)
            {
                CompanionLog.Line($"accept stop {ex.GetType().Name}: {ex.Message}");
                break;
            }
            CompanionLog.Line($"accept {client.Client.RemoteEndPoint}");
            _ = Task.Run(() => HandleClient(client, ct), ct);
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken ct)
    {
        using var _ = client;
        try
        {
            client.NoDelay = true;
            var stream = client.GetStream();
            using var headerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            headerCts.CancelAfter(TimeSpan.FromSeconds(8));
            var req = await ReadHttp(stream, headerCts.Token).ConfigureAwait(false);
            if (req is null)
            {
                CompanionLog.Line($"http 400 empty/timeout from {client.Client.RemoteEndPoint}");
                await WriteHttp(stream, 400, "Bad Request", """{"err":"bad_request"}""", ct).ConfigureAwait(false);
                return;
            }

            CompanionLog.Line($"http {req.Path} ws={req.IsWebSocket} token={(req.Token is { Length: >= 4 } t ? t[..4] + "…" : "none")} from {client.Client.RemoteEndPoint}");
            var path = req.Path.TrimEnd('/');
            if (path.Equals("/health", StringComparison.OrdinalIgnoreCase))
            {
                await WriteHttp(stream, 200, "OK", """{"ok":true,"v":1}""", ct).ConfigureAwait(false);
                return;
            }

            if (path.Equals("/v1/snapshot", StringComparison.OrdinalIgnoreCase))
            {
                if (!TokenOk(req.Token))
                {
                    await WriteHttp(stream, 401, "Unauthorized", """{"err":"token"}""", ct).ConfigureAwait(false);
                    return;
                }
                string json;
                lock (_jsonGate) json = _json;
                await WriteHttp(stream, 200, "OK", json, ct).ConfigureAwait(false);
                return;
            }

            if (path.Equals("/v1/live", StringComparison.OrdinalIgnoreCase))
            {
                if (!TokenOk(req.Token))
                {
                    CompanionLog.Line("ws 401 token mismatch");
                    await WriteHttp(stream, 401, "Unauthorized", """{"err":"token"}""", ct).ConfigureAwait(false);
                    return;
                }
                if (!req.IsWebSocket || string.IsNullOrWhiteSpace(req.WsKey))
                {
                    CompanionLog.Line("ws 426 not a websocket upgrade");
                    await WriteHttp(stream, 426, "Upgrade Required", """{"err":"websocket"}""", ct).ConfigureAwait(false);
                    return;
                }

                var accept = Convert.ToBase64String(
                    SHA1.HashData(Encoding.ASCII.GetBytes(req.WsKey.Trim() + WsMagic)));
                var switching =
                    "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: "
                    + accept + "\r\n\r\n";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(switching), ct).ConfigureAwait(false);
                await stream.FlushAsync(ct).ConfigureAwait(false);

                CompanionLog.Line("ws 101 switching protocols");
                var ws = WebSocket.CreateFromStream(
                    stream, isServer: true, subProtocol: null, keepAliveInterval: TimeSpan.FromSeconds(30));
                await Pump(ws, ct).ConfigureAwait(false);
                return;
            }

            await WriteHttp(stream, 404, "Not Found", """{"err":"not_found"}""", ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            CompanionLog.Line($"client FAIL {client.Client.RemoteEndPoint}: {ex.GetType().Name} {ex.Message}");
        }
    }

    private bool TokenOk(string? token) =>
        string.Equals(token, _token, StringComparison.Ordinal);

    private async Task Pump(WebSocket ws, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        _clients[id] = ws;
        Changed?.Invoke();
        try
        {
            string json;
            lock (_jsonGate) json = _json;
            CompanionLog.Line($"ws live clients={_clients.Count} snapshot {json.Length}b");
            await ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
            var buf = new byte[8];
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buf, ct).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close) break;
            }
        }
        catch (Exception ex)
        {
            CompanionLog.Line($"ws pump FAIL {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _clients.TryRemove(id, out _);
            try { ws.Dispose(); } catch { /* ignore */ }
            Changed?.Invoke();
        }
    }

    private static async Task WriteHttp(NetworkStream stream, int status, string reason, string json, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var head = $"HTTP/1.1 {status} {reason}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\nAccess-Control-Allow-Origin: *\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(head), ct).ConfigureAwait(false);
        await stream.WriteAsync(body, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    private sealed record HttpReq(string Path, string? Token, bool IsWebSocket, string? WsKey);

    private static async Task<HttpReq?> ReadHttp(NetworkStream stream, CancellationToken ct)
    {
        var buf = new byte[16 * 1024];
        var n = 0;
        while (n < buf.Length)
        {
            var read = await stream.ReadAsync(buf.AsMemory(n, buf.Length - n), ct).ConfigureAwait(false);
            if (read == 0) return null;
            n += read;
            var end = IndexOfHeadersEnd(buf, n);
            if (end < 0) continue;
            var text = Encoding.ASCII.GetString(buf, 0, end);
            return ParseHttp(text);
        }
        return null;
    }

    private static int IndexOfHeadersEnd(byte[] buf, int n)
    {
        for (var i = 0; i < n - 3; i++)
        {
            if (buf[i] == (byte)'\r' && buf[i + 1] == (byte)'\n' && buf[i + 2] == (byte)'\r' && buf[i + 3] == (byte)'\n')
                return i + 4;
        }
        for (var i = 0; i < n - 1; i++)
        {
            if (buf[i] == (byte)'\n' && buf[i + 1] == (byte)'\n')
                return i + 2;
        }
        return -1;
    }

    private static HttpReq? ParseHttp(string raw)
    {
        var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (lines.Length == 0) return null;
        var parts = lines[0].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        if (!Uri.TryCreate("http://pulso.local" + parts[1], UriKind.Absolute, out var uri))
            return null;

        string? token = null;
        var query = uri.Query.TrimStart('?');
        if (query.Length > 0)
        {
            foreach (var pair in query.Split('&'))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2 && kv[0] == "t")
                    token = Uri.UnescapeDataString(kv[1]);
            }
        }

        var upgrade = false;
        string? wsKey = null;
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line)) break;
            var colon = line.IndexOf(':');
            if (colon <= 0) continue;
            var name = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            if (name.Equals("Upgrade", StringComparison.OrdinalIgnoreCase)
                && value.Contains("websocket", StringComparison.OrdinalIgnoreCase))
                upgrade = true;
            if (name.Equals("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                wsKey = value;
        }

        return new HttpReq(uri.AbsolutePath, token, upgrade, wsKey);
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _tcp?.Stop(); } catch { /* ignore */ }
        _tcp = null;
        foreach (var kv in _clients)
        {
            try { kv.Value.Abort(); } catch { /* ignore */ }
        }
        _clients.Clear();
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
