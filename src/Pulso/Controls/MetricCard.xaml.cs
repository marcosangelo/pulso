using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Pulso.Hardware;

namespace Pulso.Controls;

public enum MetricCardKind
{
    /// <summary>Métrica primária 0-100% (CPU/RAM/GPU/Disco) — gauge grande + selo de status + cor de identidade.</summary>
    Gauge,
    /// <summary>Métrica secundária com escala própria (temperaturas) — gauge menor, cor por saúde.</summary>
    GaugeCompact,
    /// <summary>Métrica sem gauge percentual que faz sentido (ventoinha/trilho) — anel de status cheio, cor por saúde.</summary>
    IconStatus,
}

public partial class MetricCard : UserControl
{
    public MetricCard() => InitializeComponent();

    public string Title
    {
        get => TitleBlock.Text;
        set => TitleBlock.Text = value;
    }

    public MetricCardKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            var compact = value != MetricCardKind.Gauge;
            GaugeCell.Width = GaugeCell.Height = compact ? 72 : 104;
            Gauge.Thickness = compact ? 8 : 10;
            ValueBlock.FontSize = compact ? 16 : 22;
            StatusPill.Visibility = value == MetricCardKind.Gauge ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>Cor de identidade da métrica (ciano/magenta/âmbar/verde). Só usada quando Kind == Gauge —
    /// nos outros modos a cor sempre vem da banda de saúde (Ok/Atenção/Alto). DependencyProperty (não CLR
    /// simples) de propósito: precisa ser DynamicResource pra acompanhar a troca de tema em runtime.</summary>
    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush), typeof(Brush), typeof(MetricCard));

    public Brush? AccentBrush
    {
        get => (Brush?)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    private MetricCardKind _kind = MetricCardKind.Gauge;

    public void Update(double? value, string unit, Hint hint, string? extra = null, int digits = 0)
    {
        ValueBlock.Text = value is null ? "—" : digits == 0 ? $"{value:0}{unit}" : $"{value:0.00}{unit}";
        SubBlock.Text = extra ?? "";
        HintBlock.Text = $"{hint.Title}. {hint.Detail}";

        var bandBrush = hint.Band switch
        {
            Band.Ok => (Brush)FindResource("Ok"),
            Band.Atencao => (Brush)FindResource("Warn"),
            Band.Alto => (Brush)FindResource("Hot"),
            _ => (Brush)FindResource("Off"),
        };

        var identityColor = _kind == MetricCardKind.Gauge && AccentBrush is not null;
        var mainBrush = identityColor ? AccentBrush! : bandBrush;

        Gauge.TrackBrush = (Brush)FindResource("Line");
        Gauge.ValueBrush = mainBrush;
        Gauge.AnimateTo(_kind switch
        {
            MetricCardKind.IconStatus => 100, // anel cheio = "isto é um status, não uma escala"
            _ => value ?? 0,
        });

        BracketTL.Stroke = mainBrush;
        BracketBR.Stroke = mainBrush;
        Spark.Stroke = mainBrush;
        Spark.Push(value);

        if (_kind == MetricCardKind.Gauge)
        {
            StatusPill.Background = new SolidColorBrush(((SolidColorBrush)bandBrush).Color) { Opacity = 0.16 };
            StatusPill.BorderBrush = bandBrush;
            StatusText.Foreground = bandBrush;
            StatusText.Text = hint.Band switch
            {
                Band.Ok => "OK",
                Band.Atencao => "ATENÇÃO",
                Band.Alto => "ALTO",
                _ => "—",
            };
        }
    }
}
