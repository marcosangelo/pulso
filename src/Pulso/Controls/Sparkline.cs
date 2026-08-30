using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Pulso.Controls;

public sealed class Sparkline : FrameworkElement
{
    private readonly List<double> _points = [];
    private const int Max = 120;

    private Brush _stroke = new SolidColorBrush(Color.FromRgb(0x3E, 0xC4, 0xFF));

    public Brush Stroke
    {
        get => _stroke;
        set
        {
            _stroke = value;
            Effect = value is SolidColorBrush scb
                ? new DropShadowEffect { Color = scb.Color, BlurRadius = 7, ShadowDepth = 0, Opacity = 0.55 }
                : null;
            InvalidateVisual();
        }
    }

    public void Push(double? value)
    {
        if (value is null) { InvalidateVisual(); return; }
        _points.Add(value.Value);
        if (_points.Count > Max) _points.RemoveAt(0);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_points.Count < 2 || ActualWidth < 4 || ActualHeight < 4) return;
        var lo = _points.Min();
        var hi = _points.Max();
        var span = Math.Max(hi - lo, 1);

        Point PointAt(int i)
        {
            var x = 2 + (ActualWidth - 4) * i / (_points.Count - 1);
            var y = ActualHeight - 4 - (ActualHeight - 8) * ((_points[i] - lo) / span);
            return new Point(x, y);
        }

        var line = new StreamGeometry();
        using (var ctx = line.Open())
        {
            for (var i = 0; i < _points.Count; i++)
            {
                var p = PointAt(i);
                if (i == 0) ctx.BeginFigure(p, false, false);
                else ctx.LineTo(p, true, false);
            }
        }
        line.Freeze();

        // Preenchimento em degradê sob a linha — dá o efeito "area chart" do mockup.
        if (Stroke is SolidColorBrush strokeColor)
        {
            var fill = new StreamGeometry();
            using (var ctx = fill.Open())
            {
                ctx.BeginFigure(new Point(2, ActualHeight), true, true);
                ctx.LineTo(PointAt(0), true, false);
                for (var i = 1; i < _points.Count; i++) ctx.LineTo(PointAt(i), true, false);
                ctx.LineTo(new Point(ActualWidth - 2, ActualHeight), true, false);
            }
            fill.Freeze();

            var gradient = new LinearGradientBrush(
                Color.FromArgb(0x59, strokeColor.Color.R, strokeColor.Color.G, strokeColor.Color.B),
                Color.FromArgb(0x00, strokeColor.Color.R, strokeColor.Color.G, strokeColor.Color.B),
                new Point(0, 0), new Point(0, 1));
            dc.DrawGeometry(gradient, null, fill);
        }

        dc.DrawGeometry(null, new Pen(Stroke, 2), line);
    }
}
