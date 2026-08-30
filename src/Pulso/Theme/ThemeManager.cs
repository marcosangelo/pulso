using System.Windows;

namespace Pulso.Theme;

public enum ThemeKind
{
    CyberpunkNeon,
    StealthRed,
    ArcticPro,
    PrismRgb,
}

/// <summary>
/// Troca o dicionário de tema em runtime. Os estilos em App.xaml e as telas usam
/// DynamicResource pras chaves de cor, então re-mesclar o dicionário aqui repinta
/// a UI inteira na hora — sem precisar recriar nenhuma janela/controle.
/// </summary>
public static class ThemeManager
{
    public static ThemeKind Current { get; private set; } = ThemeKind.CyberpunkNeon;

    public static event Action? Changed;

    private static ResourceDictionary? _active;

    /// <summary>Chamar em App.OnStartup, antes de base.OnStartup(e) — precisa estar
    /// mesclado antes do MainWindow (e do próprio App.xaml.Resources) serem usados.</summary>
    public static void Initialize()
    {
        var saved = ThemeSettings.Load();
        var kind = Enum.TryParse<ThemeKind>(saved.Theme, out var parsed) ? parsed : ThemeKind.CyberpunkNeon;
        Apply(kind, persist: false);
    }

    public static void Apply(ThemeKind kind, bool persist = true)
    {
        var dict = new ResourceDictionary { Source = new Uri(SourceFor(kind), UriKind.Relative) };

        var app = Application.Current;
        if (_active is not null) app.Resources.MergedDictionaries.Remove(_active);
        app.Resources.MergedDictionaries.Add(dict);
        _active = dict;

        Current = kind;
        if (persist) new ThemeSettings { Theme = kind.ToString() }.Save();
        Changed?.Invoke();
    }

    private static string SourceFor(ThemeKind kind) => kind switch
    {
        ThemeKind.CyberpunkNeon => "Theme/CyberpunkNeon.xaml",
        ThemeKind.StealthRed => "Theme/StealthRed.xaml",
        ThemeKind.ArcticPro => "Theme/ArcticPro.xaml",
        ThemeKind.PrismRgb => "Theme/PrismRgb.xaml",
        _ => "Theme/CyberpunkNeon.xaml",
    };
}
