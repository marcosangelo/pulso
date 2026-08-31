using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Pulso.Data;
using Pulso.Hardware;
using Pulso.Link;
using Pulso.Shell;

namespace Pulso;

public partial class MainWindow : Window
{
    private readonly HardwareSampler _hw = new();
    private readonly HistoryStore _store;
    private readonly CompanionHub _hub = new();
    private readonly RelayClient _relay = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private int _ticks;
    private string _pairLink = "";
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        var db = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Pulso",
            "history.db");
        _store = new HistoryStore(db);
        _store.Prune();

        Closing += (_, e) =>
        {
            if (_allowClose) return;
            e.Cancel = true;
            Hide();
            ShowInTaskbar = false;
        };
        Loaded += (_, _) =>
        {
            NoteText.Text = "Abrindo sensores…";
            StartCompanion();
            Task.Run(() =>
            {
                try { _hw.Open(); }
                catch (Exception ex) { Dispatcher.Invoke(() => NoteText.Text = ex.Message); }
            });
        };
        Closed += (_, _) =>
        {
            _timer.Stop();
            _relay.Dispose();
            _hub.Dispose();
            _hw.Dispose();
            _store.Dispose();
        };

        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    private void Tick()
    {
        ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
        HardwareSample sample;
        try { sample = _hw.Read(); }
        catch (Exception ex)
        {
            NoteText.Text = ex.Message;
            return;
        }
        _ticks++;
        if (_ticks % 5 == 0)
        {
            try { _store.Insert(sample); }
            catch { /* histórico não pode derrubar o ao vivo */ }
            if (!_hub.IsListening)
            {
                var ips = LanAddresses.Ipv4();
                if (_hub.Start(ips))
                {
                    CompanionLog.Line("ui rebind 8742 ok");
                    RefreshPairingUi();
                }
            }
        }

        var cpuExtra = string.Join(" · ", new[]
        {
            sample.CpuName,
            sample.CpuClock is null ? null : $"{sample.CpuClock:0} MHz",
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var cpuHint = Hints.CpuLoad(sample.CpuLoad);
        var ramHint = Hints.Ram(sample.RamLoad);
        var gpuHint = Hints.GpuLoad(sample.GpuLoad);
        var diskHint = Hints.Disk(sample.DiskUsed);
        var cpuTempHint = Hints.Temp(sample.CpuTemp, "CPU");
        var gpuTempHint = Hints.Temp(sample.GpuTemp, "GPU");
        var fanHint = Hints.Fan(sample.FanRpm);
        var railHint = Hints.Rail(sample.V12, 12);

        CpuCard.Update(sample.CpuLoad, " %", cpuHint, cpuExtra);
        RamCard.Update(sample.RamLoad, " %", ramHint);
        GpuCard.Update(sample.GpuLoad, " %", gpuHint, sample.GpuName);
        DiskCard.Update(sample.DiskUsed, " %", diskHint);
        CpuTempCard.Update(sample.CpuTemp, " °C", cpuTempHint);
        GpuTempCard.Update(sample.GpuTemp, " °C", gpuTempHint);
        FanCard.Update(sample.FanRpm, " rpm", fanHint, sample.FanName);
        RailCard.Update(sample.V12, " V", railHint,
            sample.V5 is null ? null : $"5 V {sample.V5:0.00} · 3.3 V {sample.V33:0.00}", 2);

        ScorePanel.Update(Health.PulseScore.Compute(
        [
            cpuHint.Band, ramHint.Band, gpuHint.Band, diskHint.Band,
            cpuTempHint.Band, gpuTempHint.Band, fanHint.Band, railHint.Band,
        ]));

        NoteText.Text = sample.Note;
        SensorGrid.ItemsSource = sample.Sensors;
        try { DrawHistory(); }
        catch { /* gráfico vazio se o banco antigo falhar */ }
        try { _hub.Publish(sample); }
        catch { /* celular offline não derruba o desktop */ }
        try { _relay.Publish(TelemetryEnvelope.From(sample).ToJson()); }
        catch { /* relay caído não derruba o ao vivo */ }
    }

    public void AllowClose() => _allowClose = true;

    public void Reveal()
    {
        Show();
        ShowInTaskbar = true;
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }

    private void StartCompanion()
    {
        var ips = LanAddresses.Ipv4().ToList();
        var settings = Theme.ThemeSettings.Load();
        var choices = ips.ToList();
        if (!choices.Contains("10.0.2.2")) choices.Add("10.0.2.2");
        var relayHost = RelayClient.QrTarget(settings.EffectiveRelayUrl)?.Host;
        if (relayHost is not null && !choices.Contains(relayHost)) choices.Add(relayHost);
        LanBox.ItemsSource = choices;
        if (choices.Count > 0) LanBox.SelectedIndex = 0;
        if (StartWinBox is not null) StartWinBox.IsChecked = settings.StartWithWindows || AutoStart.IsEnabled();
        if (RelayBox is not null) RelayBox.Text = settings.EffectiveRelayUrl ?? "";
        _relay.Configure(settings.EffectiveRelayUrl, _hub.Token);
        var ok = _hub.Start(ips.Concat(["127.0.0.1"]));
        CompanionLog.Line(ok
            ? $"ui start ok ips={string.Join(",", ips)}"
            : $"ui start fail {_hub.LastError}");
        _hub.Changed += () => Dispatcher.Invoke(RefreshPairingUi);
        RefreshPairingUi();
        if (!ok)
            LinkStatus.Text = _hub.LastError ?? "Não abriu a porta 8742. Feche o outro Pulso.";
    }

    private void RefreshPairingUi()
    {
        if (LanBox is null || QrImage is null) return;
        var selected = LanBox.SelectedItem as string;
        var lanIps = LanAddresses.Ipv4();
        var lanHost = selected switch
        {
            "10.0.2.2" => "10.0.2.2",
            not null when lanIps.Contains(selected) => selected,
            _ => lanIps.FirstOrDefault() ?? selected ?? "127.0.0.1",
        };

        var relay = RelayClient.QrTarget(Theme.ThemeSettings.Load().EffectiveRelayUrl);
        _pairLink = relay is { } r
            ? PairingUri.Build(lanHost, CompanionHub.Port, _hub.Token, r.Host, r.Port, r.Secure)
            : PairingUri.Build(lanHost, CompanionHub.Port, _hub.Token);
        try { QrImage.Source = QrPng.Render(_pairLink); }
        catch { /* QRCoder ausente */ }
        QrCaption.Text = relay is { } rr
            ? $"Wi‑Fi {lanHost} · Ocean {rr.Host} se sair da rede"
            : lanHost switch
            {
                "127.0.0.1" or "localhost" => "Só este PC — escolha a Wi‑Fi",
                "10.0.2.2" => "Emulador Android neste PC",
                _ => lanHost,
            };
        LinkStatus.Text = $"{_hub.ClientCount} celular(es) · porta {CompanionHub.Port} · token {_hub.Token[..4]}…";
        if (LogPathText is not null)
            LogPathText.Text = $"Log: {CompanionLog.Path}";
    }

    private void OnLanChanged(object sender, SelectionChangedEventArgs e) => RefreshPairingUi();

    private void OnRotateQr(object sender, RoutedEventArgs e)
    {
        _hub.RotateToken();
        RefreshPairingUi();
    }

    private void OnOpenLinkLog(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = CompanionLog.Path;
            if (!System.IO.File.Exists(path))
                CompanionLog.Line("arquivo criado pelo botão Abrir log");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            LinkStatus.Text = $"Não abriu o log: {ex.Message}";
        }
    }

    private void OnStartWithWindows(object sender, RoutedEventArgs e)
    {
        AutoStart.Set(StartWinBox.IsChecked == true);
    }

    private void OnApplyRelay(object sender, RoutedEventArgs e)
    {
        var url = RelayBox.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            Theme.ThemeSettings.Update(s => s.RelayUrl = "");
            _relay.Configure(null, _hub.Token);
            RebuildLanChoices(select: null);
            LinkStatus.Text = "Relay desligado. QR volta a ser LAN.";
            return;
        }
        if (RelayClient.TryParseBase(url) is null)
        {
            LinkStatus.Text = "URL do relay inválida. Ex.: ws://IP:8080 ou wss://dominio";
            return;
        }
        Theme.ThemeSettings.Update(s => s.RelayUrl = url);
        _relay.Configure(url, _hub.Token);
        var host = RelayClient.QrTarget(url)!.Value.Host;
        RebuildLanChoices(select: host);
        LinkStatus.Text = $"Relay {url} — o PC publica de saída. Escolha {host} na lista do QR.";
    }

    private void RebuildLanChoices(string? select)
    {
        var ips = LanAddresses.Ipv4().ToList();
        var choices = ips.ToList();
        if (!choices.Contains("10.0.2.2")) choices.Add("10.0.2.2");
        var relayHost = RelayClient.QrTarget(Theme.ThemeSettings.Load().EffectiveRelayUrl)?.Host;
        if (relayHost is not null && !choices.Contains(relayHost)) choices.Add(relayHost);
        LanBox.ItemsSource = choices;
        if (select is not null && choices.Contains(select)) LanBox.SelectedItem = select;
        else if (choices.Count > 0) LanBox.SelectedIndex = 0;
        RefreshPairingUi();
    }

    private void OnCopyLink(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_pairLink)) return;
        Clipboard.SetText(_pairLink);
        LinkStatus.Text = "Link copiado. Cole no app se a câmera falhar.";
    }

    private void OnHistoryChanged(object sender, SelectionChangedEventArgs e) => DrawHistory();
    private void OnHistSize(object sender, SizeChangedEventArgs e) => DrawHistory();

    private void DrawHistory()
    {
        if (HistCanvas is null || SeriesBox?.SelectedItem is not ComboBoxItem seriesItem) return;
        var col = seriesItem.Tag as string ?? "cpu_pct";
        var hours = 1.0;
        if (RangeBox?.SelectedItem is ComboBoxItem rangeItem && double.TryParse(rangeItem.Tag?.ToString(), out var h))
            hours = h;

        var rows = _store.Query(col, DateTimeOffset.Now.AddHours(-hours));
        HistCanvas.Children.Clear();
        PeakText.Text = $"{_store.Count()} amostras";
        if (rows.Count < 2 || HistCanvas.ActualWidth < 20) return;

        var vals = rows.Select(r => r.Value).Where(v => v is not null).Select(v => v!.Value).ToList();
        if (vals.Count == 0) return;
        PeakText.Text = $"pico {vals.Max():0.#}   ·   {_store.Count()} amostras";

        var lo = vals.Min();
        var hi = vals.Max();
        var span = Math.Max(hi - lo, 1);
        var w = HistCanvas.ActualWidth;
        var hgt = HistCanvas.ActualHeight;
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            var first = true;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Value is null) continue;
                var x = w * i / (rows.Count - 1);
                var y = hgt - 8 - (hgt - 16) * ((rows[i].Value!.Value - lo) / span);
                if (first) { ctx.BeginFigure(new Point(x, y), false, false); first = false; }
                else ctx.LineTo(new Point(x, y), true, false);
            }
        }
        var path = new System.Windows.Shapes.Path
        {
            Data = geo,
            Stroke = (Brush)FindResource("Accent"),
            StrokeThickness = 2,
        };
        HistCanvas.Children.Add(path);
    }
}
