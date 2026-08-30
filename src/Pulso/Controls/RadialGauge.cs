using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Pulso.Controls;

/// <summary>
/// Gauge circular estilo velocímetro: arco de 270° (abertura de 90° embaixo),
/// track + arco de valor com pontas arredondadas. Não desenha o número central —
/// isso fica por conta de um TextBlock sobreposto em quem usa o controle.
/// </summary>
public sealed class RadialGauge : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(RadialGauge),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public Brush TrackBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
    public Brush ValueBrush { get; set; } = Brushes.White;
    public double Thickness { get; set; } = 10;

    private const double StartAngleDeg = 225; // 0° = topo, sentido horário
    private const double SweepAngleDeg = 270; // abertura de 90° embaixo

    /// <summary>Anima suavemente até o novo valor (0-100) em vez de saltar a cada tick.</summary>
    public void AnimateTo(double target)
    {
        target = Math.Clamp(target, 0, 100);
        var anim = new DoubleAnimation(target, TimeSpan.FromMilliseconds(450))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        BeginAnimation(ValueProperty, anim);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w < 8 || h < 8) return;

        var thickness = Thickness;
        var cx = w / 2;
        var cy = h / 2;
        var r = Math.Min(w, h) / 2 - thickness / 2 - 1;
        if (r <= 0) return;

        var trackPen = new Pen(TrackBrush, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawGeometry(null, trackPen, ArcGeometry(cx, cy, r, StartAngleDeg, SweepAngleDeg));

        var pct = Math.Clamp(Value, 0, 100) / 100.0;
        var sweep = SweepAngleDeg * pct;
        if (sweep > 0.5)
        {
            var valuePen = new Pen(ValueBrush, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            dc.DrawGeometry(null, valuePen, ArcGeometry(cx, cy, r, StartAngleDeg, sweep));
        }
    }

    private static Point PointOnCircle(double cx, double cy, double r, double angleDeg)
    {
        var rad = angleDeg * Math.PI / 180.0;
        return new Point(cx + r * Math.Sin(rad), cy - r * Math.Cos(rad));
    }

    private static Geometry ArcGeometry(double cx, double cy, double r, double startDeg, double sweepDeg)
    {
        var start = PointOnCircle(cx, cy, r, startDeg);
        var end = PointOnCircle(cx, cy, r, startDeg + sweepDeg);
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(start, false, false);
            ctx.ArcTo(end, new Size(r, r), 0, sweepDeg > 180, SweepDirection.Clockwise, true, false);
        }
        geo.Freeze();
        return geo;
    }
}
