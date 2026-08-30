using System.Windows;
using System.Windows.Controls;
using Pulso.Theme;

namespace Pulso.Controls;

public partial class ThemeSwitcher : UserControl
{
    public ThemeSwitcher()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncSelection();
    }

    private void SyncSelection()
    {
        var name = ThemeManager.Current.ToString();
        foreach (var rb in new[] { OptNeon, OptStealth, OptArctic, OptPrism })
            rb.IsChecked = (string)rb.Tag == name;
    }

    private void OnPick(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb) return;
        if (Enum.TryParse<ThemeKind>((string)rb.Tag, out var kind))
        {
            ThemeManager.Apply(kind);
        }
        Flyout.IsOpen = false;
    }
}
