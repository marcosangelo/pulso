using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pulso.Controls;

public sealed class Sparkline : FrameworkElement
{
    private readonly List<double> _points = [];
    private const int Max = 120;

    public Brush Stroke { get; set; } = new SolidColorBrush(Color.FromRgb(0x3E, 0xC4, 0xFF));

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
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            for (var i = 0; i < _points.Count; i++)
            {
                var x = 2 + (ActualWidth - 4) * i / (_points.Count - 1);
                var y = ActualHeight - 4 - (ActualHeight - 8) * ((_points[i] - lo) / span);
                if (i == 0) ctx.BeginFigure(new Point(x, y), false, false);
                else ctx.LineTo(new Point(x, y), true, false);
            }
        }
        geo.Freeze();
        dc.DrawGeometry(null, new Pen(Stroke, 2), geo);
    }
}
