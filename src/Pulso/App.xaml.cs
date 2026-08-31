using System.IO;
using System.Windows;
using System.Windows.Threading;
using Pulso.Shell;

namespace Pulso;

public partial class App : Application
{
    private SingleInstance? _single;
    private TrayIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnUiException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
        TaskScheduler.UnobservedTaskException += OnTaskException;

        _single = new SingleInstance();
        if (!_single.OwnsProcess)
        {
            SingleInstance.SignalShow();
            Shutdown();
            return;
        }

        try { Theme.ThemeManager.Initialize(); }
        catch (Exception ex) { Log(ex); }

        var dash = new MainWindow();
        MainWindow = dash;
        _single.Listen(() => Dispatcher.BeginInvoke(() => dash.Reveal()));
        _tray = new TrayIcon(() => dash.Reveal(), RequestExit);

        var trayStart = e.Args.Any(a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase));
        if (trayStart)
        {
            dash.ShowInTaskbar = false;
            dash.Hide();
            _tray.Tip("Pulso", "Hub na porta 8742. Duplo clique na bandeja abre o painel.");
        }
        else
            dash.Show();

        base.OnStartup(e);
    }

    public void RequestExit()
    {
        if (MainWindow is MainWindow w)
        {
            w.AllowClose();
            w.Close();
        }
        _tray?.Dispose();
        _single?.Dispose();
        Shutdown();
    }

    private static void OnUiException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log(e.Exception);
        e.Handled = true;
    }

    private static void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex) Log(ex);
    }

    private static void OnTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log(e.Exception);
        e.SetObserved();
    }

    private static void Log(Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Pulso");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "crash.log"),
                $"{DateTime.Now:u}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }
    }
}
