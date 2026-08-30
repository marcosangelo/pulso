using System.Text.Json;
using System.Text.Json.Serialization;
using Pulso.Hardware;

namespace Pulso.Link;

/// <summary>Contrato v1 Pulso ↔ app. Campos novos entram como opcionais; nunca quebrar v.</summary>
public sealed class TelemetryEnvelope
{
    [JsonPropertyName("v")] public int Version { get; init; } = 1;
    [JsonPropertyName("at")] public long At { get; init; }
    [JsonPropertyName("cpu")] public CpuBlock Cpu { get; init; } = new();
    [JsonPropertyName("gpu")] public GpuBlock Gpu { get; init; } = new();
    [JsonPropertyName("ram")] public RamBlock Ram { get; init; } = new();
    [JsonPropertyName("disk")] public DiskBlock Disk { get; init; } = new();
    [JsonPropertyName("fan")] public FanBlock Fan { get; init; } = new();
    [JsonPropertyName("rails")] public RailsBlock Rails { get; init; } = new();

    public sealed class CpuBlock
    {
        [JsonPropertyName("load")] public double? Load { get; init; }
        [JsonPropertyName("temp")] public double? Temp { get; init; }
        [JsonPropertyName("clock")] public double? Clock { get; init; }
        [JsonPropertyName("name")] public string? Name { get; init; }
    }

    public sealed class GpuBlock
    {
        [JsonPropertyName("load")] public double? Load { get; init; }
        [JsonPropertyName("temp")] public double? Temp { get; init; }
        [JsonPropertyName("name")] public string? Name { get; init; }
    }

    public sealed class RamBlock
    {
        [JsonPropertyName("load")] public double? Load { get; init; }
    }

    public sealed class DiskBlock
    {
        [JsonPropertyName("used")] public double? Used { get; init; }
        [JsonPropertyName("temp")] public double? Temp { get; init; }
    }

    public sealed class FanBlock
    {
        [JsonPropertyName("rpm")] public double? Rpm { get; init; }
        [JsonPropertyName("name")] public string? Name { get; init; }
    }

    public sealed class RailsBlock
    {
        [JsonPropertyName("v12")] public double? V12 { get; init; }
        [JsonPropertyName("v5")] public double? V5 { get; init; }
        [JsonPropertyName("v33")] public double? V33 { get; init; }
    }

    public static TelemetryEnvelope From(HardwareSample s) => new()
    {
        At = s.At.ToUnixTimeMilliseconds(),
        Cpu = new CpuBlock { Load = s.CpuLoad, Temp = s.CpuTemp, Clock = s.CpuClock, Name = s.CpuName },
        Gpu = new GpuBlock { Load = s.GpuLoad, Temp = s.GpuTemp, Name = s.GpuName },
        Ram = new RamBlock { Load = s.RamLoad },
        Disk = new DiskBlock { Used = s.DiskUsed, Temp = s.StorageTemp },
        Fan = new FanBlock { Rpm = s.FanRpm, Name = s.FanName },
        Rails = new RailsBlock { V12 = s.V12, V5 = s.V5, V33 = s.V33 },
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOpts);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
