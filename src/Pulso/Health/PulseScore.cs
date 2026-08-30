using Pulso.Hardware;

namespace Pulso.Health;

/// <summary>
/// Nota agregada 0-100 do sistema, calculada a cada tick a partir das 8 bandas de saúde
/// que já existem (Hints.*). Não existia antes — é a heurística inicial usada pelo hero
/// "Pulso Score" do dashboard; fácil de recalibrar depois sem mexer em Hints.cs.
/// </summary>
public static class PulseScore
{
    private const int PenaltyAtencao = 6;
    private const int PenaltyAlto = 16;

    public static int Compute(IReadOnlyList<Band> bands)
    {
        var score = 100;
        foreach (var band in bands)
        {
            score -= band switch
            {
                Band.Atencao => PenaltyAtencao,
                Band.Alto => PenaltyAlto,
                _ => 0, // Ok e Off (sensor ausente) não penalizam
            };
        }
        return Math.Clamp(score, 0, 100);
    }
}
