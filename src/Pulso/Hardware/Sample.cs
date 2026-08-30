namespace Pulso.Hardware;

public sealed class HardwareSample
{
    public DateTimeOffset At { get; init; } = DateTimeOffset.Now;
    public double? CpuLoad { get; init; }
    public double? CpuTemp { get; init; }
    public double? CpuClock { get; init; }
    public string? CpuName { get; init; }
    public double? RamLoad { get; init; }
    public double? GpuLoad { get; init; }
    public double? GpuTemp { get; init; }
    public string? GpuName { get; init; }
    public double? DiskUsed { get; init; }
    public double? StorageTemp { get; init; }
    public double? FanRpm { get; init; }
    public string? FanName { get; init; }
    public double? V12 { get; init; }
    public double? V5 { get; init; }
    public double? V33 { get; init; }
    public IReadOnlyList<SensorRow> Sensors { get; init; } = [];
    public string Note { get; init; } = "";
}

public sealed record SensorRow(string Hardware, string Name, string Type, double? Value, string? Unit);
