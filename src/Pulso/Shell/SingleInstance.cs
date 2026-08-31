using System.Threading;

namespace Pulso.Shell;

/// <summary>Um Pulso só — o segundo clique só acorda a janela (evita porta 8742 ocupada).</summary>
internal sealed class SingleInstance : IDisposable
{
    public const string MutexName = @"Local\Pulso.SingleInstance";
    public const string ShowEventName = @"Local\Pulso.ShowWindow";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _show;
    private readonly CancellationTokenSource _cts = new();
    private readonly bool _owns;

    public bool OwnsProcess => _owns;

    public SingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out _owns);
        _show = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
    }

    public static void SignalShow()
    {
        try
        {
            using var ev = EventWaitHandle.OpenExisting(ShowEventName);
            ev.Set();
        }
        catch
        {
            // dono ainda não criou o evento
        }
    }

    public void Listen(Action onShow)
    {
        _ = Task.Run(() =>
        {
            while (!_cts.IsCancellationRequested)
            {
                if (_show.WaitOne(TimeSpan.FromSeconds(1)))
                {
                    try { onShow(); } catch { /* ignore */ }
                }
            }
        }, _cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { if (_owns) _mutex.ReleaseMutex(); } catch { /* ignore */ }
        _mutex.Dispose();
        _show.Dispose();
        _cts.Dispose();
    }
}
