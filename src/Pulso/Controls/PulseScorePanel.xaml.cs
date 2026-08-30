using System.Windows.Controls;
using System.Windows.Media;

namespace Pulso.Controls;

public partial class PulseScorePanel : UserControl
{
    public PulseScorePanel() => InitializeComponent();

    public void Update(int score)
    {
        ScoreText.Text = score.ToString();

        var (brush, caption) = score switch
        {
            >= 80 => ((Brush)FindResource("Ok"), "Sistema saudável"),
            >= 50 => ((Brush)FindResource("Warn"), "Alguma coisa pede atenção"),
            _ => ((Brush)FindResource("Hot"), "Vale checar os cards em alerta"),
        };

        Gauge.TrackBrush = (Brush)FindResource("Line");
        Gauge.ValueBrush = brush;
        Gauge.AnimateTo(score);

        ScoreCaption.Foreground = brush;
        ScoreCaption.Text = caption;

        Wave.Stroke = brush;
        Wave.Push(score);
    }
}
