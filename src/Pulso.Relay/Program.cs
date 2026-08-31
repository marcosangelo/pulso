using System.Collections.Concurrent;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
var urls = Environment.GetEnvironmentVariable("PULSO_RELAY_BIND") ?? "http://0.0.0.0:8080";
builder.WebHost.UseUrls(urls);

var app = builder.Build();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

var rooms = new ConcurrentDictionary<string, Room>(StringComparer.Ordinal);

app.MapGet("/health", () => Results.Json(new { ok = true, v = 1, rooms = rooms.Count }));

app.Map("/v1/up", ctx => Accept(ctx, rooms, publisher: true));
app.Map("/v1/live", ctx => Accept(ctx, rooms, publisher: false));

app.Run();

static async Task Accept(HttpContext ctx, ConcurrentDictionary<string, Room> rooms, bool publisher)
{
    var token = ctx.Request.Query["t"].ToString();
    if (string.IsNullOrWhiteSpace(token) || token.Length < 8)
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsJsonAsync(new { err = "token" });
        return;
    }
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 426;
        await ctx.Response.WriteAsJsonAsync(new { err = "websocket" });
        return;
    }

    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    var room = rooms.GetOrAdd(token, _ => new Room());
    if (publisher)
    {
        var old = Interlocked.Exchange(ref room.Pc, ws);
        if (old is not null && !ReferenceEquals(old, ws))
        {
            try { old.Abort(); } catch { /* ignore */ }
        }
    }
    else
    {
        room.Phones.TryAdd(ws, 0);
        var snap = room.Last;
        if (!string.IsNullOrEmpty(snap) && ws.State == WebSocketState.Open)
            await ws.SendAsync(System.Text.Encoding.UTF8.GetBytes(snap), WebSocketMessageType.Text, true, ctx.RequestAborted);
    }

    var buf = new byte[64 * 1024];
    try
    {
        while (ws.State == WebSocketState.Open && !ctx.RequestAborted.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(buf, ctx.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close) return;
                ms.Write(buf, 0, result.Count);
            } while (!result.EndOfMessage);

            if (!publisher) continue;
            var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            room.Last = json;
            var bytes = ms.ToArray();
            foreach (var phone in room.Phones.Keys)
            {
                if (phone.State != WebSocketState.Open)
                {
                    room.Phones.TryRemove(phone, out _);
                    continue;
                }
                try
                {
                    await phone.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    room.Phones.TryRemove(phone, out _);
                }
            }
        }
    }
    catch
    {
        // socket caiu
    }
    finally
    {
        if (publisher && ReferenceEquals(room.Pc, ws))
            Interlocked.CompareExchange(ref room.Pc, null, ws);
        room.Phones.TryRemove(ws, out _);
    }
}

sealed class Room
{
    public WebSocket? Pc;
    public string? Last;
    public ConcurrentDictionary<WebSocket, byte> Phones { get; } = new();
}
