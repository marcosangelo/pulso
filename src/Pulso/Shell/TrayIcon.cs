using System.Drawing;
using System.Windows.Forms;

namespace Pulso.Shell;

internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notify;

    public TrayIcon(Action show, Action exit)
    {
        _notify = new NotifyIcon
        {
            Text = "Pulso — hub na 8742",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip(),
        };
        _notify.ContextMenuStrip.Items.Add("Abrir painel", null, (_, _) => show());
        _notify.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        _notify.ContextMenuStrip.Items.Add("Sair (para o hub)", null, (_, _) => exit());
        _notify.DoubleClick += (_, _) => show();
    }

    public void Tip(string title, string text)
    {
        try { _notify.ShowBalloonTip(2500, title, text, ToolTipIcon.Info); } catch { /* ignore */ }
    }

    public void Dispose()
    {
        _notify.Visible = false;
        _notify.Dispose();
    }
}
