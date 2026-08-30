using System.IO;
using LibreHardwareMonitor.Hardware;

namespace Pulso.Hardware;

public sealed class HardwareSampler : IDisposable
{
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true,
        IsControllerEnabled = true,
        IsStorageEnabled = true,
        IsNetworkEnabled = false,
        IsPsuEnabled = true,
        IsPowerMonitorEnabled = true,
    };

    private readonly UpdateVisitor _visitor = new();
    private readonly object _gate = new();
    private bool _opened;

    public void Open()
    {
        lock (_gate)
        {
            if (_opened) return;
            _computer.Open();
            _opened = true;
        }
    }

    public HardwareSample Read()
    {
        lock (_gate)
        {
            if (!_opened) Open();
            _computer.Accept(_visitor);

            var sensors = new List<SensorRow>();
            Walk(_computer.Hardware, sensors);

            double? Pick(SensorType type, Func<SensorRow, bool>? pred = null, bool max = false)
            {
                var hits = sensors.Where(s => s.Type == type.ToString() && s.Value is not null);
                if (pred != null) hits = hits.Where(pred);
                var list = hits.ToList();
                if (list.Count == 0) return null;
                return max ? list.Max(s => s.Value) : list[0].Value;
            }

            static bool NameHas(SensorRow s, params string[] parts) =>
                parts.Any(p => s.Name.Contains(p, StringComparison.OrdinalIgnoreCase)
                               || s.Hardware.Contains(p, StringComparison.OrdinalIgnoreCase));

            var cpuTemp = Pick(SensorType.Temperature, s => NameHas(s, "cpu", "package", "tctl", "tdie", "core"))
                          ?? Pick(SensorType.Temperature, s => s.Hardware.Contains("CPU", StringComparison.OrdinalIgnoreCase));
            var gpuTemp = Pick(SensorType.Temperature, s => NameHas(s, "gpu", "core"));
            var ssdTemp = Pick(SensorType.Temperature, s => NameHas(s, "nvme", "ssd", "hdd", "drive", "storage"), max: true);
            var cpuLoad = Pick(SensorType.Load, s => NameHas(s, "cpu") && NameHas(s, "total"))
                          ?? Pick(SensorType.Load, s => NameHas(s, "cpu"));
            var gpuLoad = Pick(SensorType.Load, s => NameHas(s, "gpu") && (NameHas(s, "core") || NameHas(s, "total")))
                          ?? Pick(SensorType.Load, s => NameHas(s, "gpu"));
            var ramLoad = Pick(SensorType.Load, s => NameHas(s, "memory") && !NameHas(s, "gpu", "video"));
            var fan = sensors
                .Where(s => s.Type == nameof(SensorType.Fan) && s.Value is not null)
                .OrderByDescending(s => s.Value)
                .FirstOrDefault();
            var v12 = PickVoltage(sensors, 12, 2.4);
            var v5 = PickVoltage(sensors, 5, 1.1);
            var v33 = PickVoltage(sensors, 3.3, 0.7);
            var clock = Pick(SensorType.Clock, s => NameHas(s, "cpu") && NameHas(s, "core"), max: true);

            var cpuHw = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            var gpuHw = _computer.Hardware.FirstOrDefault(h =>
                h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel);

            double? diskUsed = null;
            try
            {
                var d = new DriveInfo("C");
                if (d.IsReady)
                    diskUsed = 100.0 * (d.TotalSize - d.TotalFreeSpace) / d.TotalSize;
            }
            catch
            {
                // ignore
            }

            var fanSensors = sensors.Count(s => s.Type == nameof(SensorType.Fan));
            var voltSensors = sensors.Count(s => s.Type == nameof(SensorType.Voltage));
            var admin = Privileges.IsAdministrator();
            var missing = new List<string>();
            if (cpuTemp is null) missing.Add("temp CPU");
            if (fan is null) missing.Add("fan");
            if (v12 is null) missing.Add("12 V");
            string note;
            if (missing.Count == 0)
            {
                note = $"LibreHardwareMonitor · {sensors.Count} sensores";
            }
            else if (!admin)
            {
                note = $"LibreHardwareMonitor · {sensors.Count} sensores · sem {string.Join(", ", missing)} — abra como administrador";
            }
            else if (fanSensors == 0 && voltSensors == 0)
            {
                note = $"LibreHardwareMonitor · {sensors.Count} sensores · Super I/O ausente (0 Fan, 0 Voltage). Já é admin. Aba Sensores · Integridade da memória pode bloquear o driver";
            }
            else
            {
                note = $"LibreHardwareMonitor · {sensors.Count} sensores · {fanSensors} fan · {voltSensors} tensões · sem {string.Join(", ", missing)} — a placa não mapeou esse trilho/header";
            }

            return new HardwareSample
            {
                CpuLoad = cpuLoad,
                CpuTemp = cpuTemp,
                CpuClock = clock,
                CpuName = cpuHw?.Name,
                RamLoad = ramLoad,
                GpuLoad = gpuLoad,
                GpuTemp = gpuTemp,
                GpuName = gpuHw?.Name,
                DiskUsed = diskUsed,
                StorageTemp = ssdTemp,
                FanRpm = fan?.Value,
                FanName = fan?.Name,
                V12 = v12,
                V5 = v5,
                V33 = v33,
                Sensors = sensors,
                Note = note,
            };
        }
    }

    private static double? PickVoltage(IEnumerable<SensorRow> sensors, double nominal, double window)
    {
        var volts = sensors
            .Where(s => s.Type == nameof(SensorType.Voltage) && s.Value is not null)
            .Select(s => NormalizeVolts(s.Value!.Value));

        var named = sensors.Where(s =>
            s.Type == nameof(SensorType.Voltage) &&
            s.Value is not null &&
            (s.Name.Contains($"+{nominal}", StringComparison.OrdinalIgnoreCase)
             || s.Name.Contains($"{nominal:0.0}V", StringComparison.OrdinalIgnoreCase)
             || s.Name.Contains($"{nominal:g}V", StringComparison.OrdinalIgnoreCase)
             || (nominal == 12 && (s.Name.Contains("12V", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("12 V", StringComparison.OrdinalIgnoreCase)))
             || (nominal == 5 && (s.Name.Contains("+5V", StringComparison.OrdinalIgnoreCase) || s.Name.Equals("5V", StringComparison.OrdinalIgnoreCase)))
             || (Math.Abs(nominal - 3.3) < 0.01 && (s.Name.Contains("3.3", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("3V3", StringComparison.OrdinalIgnoreCase)))));
        var hit = named.Select(s => NormalizeVolts(s.Value!.Value)).FirstOrDefault();
        if (hit != 0) return hit;

        var nearby = volts
            .Where(v => Math.Abs(v - nominal) <= window)
            .OrderBy(v => Math.Abs(v - nominal))
            .ToList();
        return nearby.Count == 0 ? null : nearby[0];
    }

    /// <summary>Alguns Super I/O entregam milivolts (12000) em vez de 12.0 V.</summary>
    private static double NormalizeVolts(double v) => v is > 20 and < 20_000 ? v / 1000.0 : v;

    private static void Walk(IEnumerable<IHardware> hardware, List<SensorRow> into)
    {
        foreach (var hw in hardware)
        {
            foreach (var s in hw.Sensors)
            {
                into.Add(new SensorRow(
                    hw.Name,
                    s.Name,
                    s.SensorType.ToString(),
                    s.Value,
                    UnitOf(s.SensorType)));
            }
            if (hw.SubHardware.Length > 0)
                Walk(hw.SubHardware, into);
        }
    }

    private static string UnitOf(SensorType t) => t switch
    {
        SensorType.Temperature => "°C",
        SensorType.Load => "%",
        SensorType.Fan => "rpm",
        SensorType.Voltage => "V",
        SensorType.Clock => "MHz",
        SensorType.Power => "W",
        SensorType.Data => "GB",
        SensorType.SmallData => "MB",
        SensorType.Throughput => "MB/s",
        SensorType.Flow => "L/h",
        _ => "",
    };

    public void Dispose()
    {
        lock (_gate)
        {
            if (_opened)
                _computer.Close();
            _opened = false;
        }
    }
}
