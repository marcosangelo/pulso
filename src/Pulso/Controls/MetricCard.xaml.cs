using System.Windows.Controls;
using System.Windows.Media;
using Pulso.Hardware;

namespace Pulso.Controls;

public partial class MetricCard : UserControl
{
    public MetricCard() => InitializeComponent();

    public string Title
    {
        get => TitleBlock.Text;
        set => TitleBlock.Text = value;
    }

    public void Update(double? value, string unit, Hint hint, string? extra = null, int digits = 0)
    {
        ValueBlock.Text = value is null ? "—" : digits == 0 ? $"{value:0}{unit}" : $"{value:0.00}{unit}";
        SubBlock.Text = extra ?? "";
        HintBlock.Text = $"{hint.Title}. {hint.Detail}";
        var brush = hint.Band switch
        {
            Band.Ok => (Brush)FindResource("Ok"),
            Band.Atencao => (Brush)FindResource("Warn"),
            Band.Alto => (Brush)FindResource("Hot"),
            _ => (Brush)FindResource("Off"),
        };
        Stripe.Background = brush;
        Spark.Stroke = brush;
        Spark.Push(value);
    }
}
