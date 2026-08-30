namespace Pulso.Hardware;

public enum Band { Ok, Atencao, Alto, Off }

public readonly record struct Hint(Band Band, string Title, string Detail);

public static class Hints
{
    public static Hint CpuLoad(double? v) => v switch
    {
        null => Off("CPU sem carga", "O sensor de load não apareceu."),
        < 70 => Ok("CPU folgada", "Sobrou cabeça. Uso alto só preocupa se for contínuo e quente."),
        < 92 => Aten("CPU ocupada", "Normal em jogo ou compile. Se estiver ocioso assim, veja o segundo plano."),
        _ => Alto("CPU no teto", "100% por vários minutos esquenta e limita frequência."),
    };

    public static Hint Ram(double? v) => v switch
    {
        null => Off("RAM sem dado", "O Windows não entregou a memória."),
        < 80 => Ok("RAM confortável", "Ainda tem folga."),
        < 93 => Aten("RAM apertada", "Perto do limite o Windows usa disco e o PC fica pesado."),
        _ => Alto("RAM esgotada", "Feche abas/apps ou avalie mais memória."),
    };

    public static Hint GpuLoad(double? v) => v switch
    {
        null => Off("GPU sem load", "Driver ainda não expôs o sensor."),
        < 80 => Ok("GPU em ritmo", "No desktop o uso baixo é esperado."),
        < 98 => Aten("GPU trabalhando", "Típico em jogo. Cruze com a temperatura."),
        _ => Alto("GPU no limite", "Saturada. Se a temp também estiver alta, baixe o gráfico."),
    };

    public static Hint Temp(double? c, string kind) => c switch
    {
        null => Off($"{kind} sem sensor", "Rode como administrador se a placa esconde o Super I/O."),
        < 75 => Ok($"{kind} fresco", "Faixa boa para uso contínuo."),
        < 90 => Aten($"{kind} quente", "Comum em carga. 80–90 °C o dia todo no silêncio merece um olhar no cooler."),
        _ => Alto($"{kind} muito quente", "Acima de 90 °C contínuo. Pasta, dissipador, poeira."),
    };

    public static Hint Fan(double? rpm) => rpm switch
    {
        null => Off("Fan invisível", "Só header da placa ou hub. Molex/SATA puro não reporta."),
        < 200 => Alto("Quase parada", "RPM baixo com máquina quente: cabo, poeira ou fan morta."),
        < 2500 => Ok("Ar circulando", "A curva sobe o giro quando esquenta."),
        _ => Aten("Fan alta", "Muito ruído. Se a temp já baixou, a curva da BIOS está agressiva."),
    };

    public static Hint Rail(double? volts, double nominal) =>
        volts is null
            ? Off("Trilho ausente", "Leitura da placa (LPC/EC), não um voltímetro na fonte. Fonte genérica não fala com o Windows.")
            : Math.Abs(volts.Value - nominal) / nominal <= 0.05
                ? Ok($"Trilho {nominal:g} V", $"{volts:0.00} V — dentro de ±5% ATX. Sensor da placa, com erro.")
                : Math.Abs(volts.Value - nominal) / nominal <= 0.08
                    ? Aten("Trilho oscilando", $"{volts:0.00} V vs {nominal:g} V.")
                    : Alto("Trilho fora", $"{volts:0.00} V vs {nominal:g} V. Pode ser sensor ou fonte — não conclua só por isto.");

    public static Hint Disk(double? v) => v switch
    {
        null => Off("Disco sem dado", "Volume C: não lido."),
        < 85 => Ok("Disco com folga", "SSD precisa de espaço livre."),
        < 95 => Aten("Disco cheio", "Acima de ~85% o SSD perde um pouco."),
        _ => Alto("Disco no limite", "Quase sem espaço."),
    };

    private static Hint Ok(string t, string d) => new(Band.Ok, t, d);
    private static Hint Aten(string t, string d) => new(Band.Atencao, t, d);
    private static Hint Alto(string t, string d) => new(Band.Alto, t, d);
    private static Hint Off(string t, string d) => new(Band.Off, t, d);
}
