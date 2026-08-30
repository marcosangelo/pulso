using System.IO;
using Microsoft.Data.Sqlite;
using Pulso.Hardware;

namespace Pulso.Data;

public sealed class HistoryStore : IDisposable
{
    private readonly SqliteConnection _db;

    public HistoryStore(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _db = new SqliteConnection($"Data Source={path}");
        _db.Open();
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS samples (
              ts REAL PRIMARY KEY,
              cpu_pct REAL, ram_pct REAL, gpu_pct REAL, disk_pct REAL,
              cpu_temp REAL, gpu_temp REAL, ssd_temp REAL, fan_rpm REAL,
              v12 REAL, v5 REAL, v33 REAL
            );
            """;
        cmd.ExecuteNonQuery();
        EnsureColumn("ssd_temp");
        EnsureColumn("cpu_pct");
        EnsureColumn("ram_pct");
        EnsureColumn("gpu_pct");
        EnsureColumn("disk_pct");
        EnsureColumn("fan_rpm");
    }

    private void EnsureColumn(string name)
    {
        using var check = _db.CreateCommand();
        check.CommandText = "PRAGMA table_info(samples)";
        using var r = check.ExecuteReader();
        while (r.Read())
        {
            if (string.Equals(r.GetString(1), name, StringComparison.OrdinalIgnoreCase))
                return;
        }
        r.Close();
        using var alter = _db.CreateCommand();
        alter.CommandText = $"ALTER TABLE samples ADD COLUMN {name} REAL";
        alter.ExecuteNonQuery();
    }

    public void Insert(HardwareSample s)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO samples
            (ts,cpu_pct,ram_pct,gpu_pct,disk_pct,cpu_temp,gpu_temp,ssd_temp,fan_rpm,v12,v5,v33)
            VALUES ($ts,$cpu,$ram,$gpu,$disk,$ct,$gt,$st,$fan,$v12,$v5,$v33);
            """;
        cmd.Parameters.AddWithValue("$ts", s.At.ToUnixTimeSeconds());
        Bind(cmd, "$cpu", s.CpuLoad);
        Bind(cmd, "$ram", s.RamLoad);
        Bind(cmd, "$gpu", s.GpuLoad);
        Bind(cmd, "$disk", s.DiskUsed);
        Bind(cmd, "$ct", s.CpuTemp);
        Bind(cmd, "$gt", s.GpuTemp);
        Bind(cmd, "$st", s.StorageTemp);
        Bind(cmd, "$fan", s.FanRpm);
        Bind(cmd, "$v12", s.V12);
        Bind(cmd, "$v5", s.V5);
        Bind(cmd, "$v33", s.V33);
        cmd.ExecuteNonQuery();
    }

    public void Prune(int days = 30)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "DELETE FROM samples WHERE ts < $cut";
        cmd.Parameters.AddWithValue("$cut", DateTimeOffset.Now.AddDays(-days).ToUnixTimeSeconds());
        cmd.ExecuteNonQuery();
    }

    public List<(DateTime At, double? Value)> Query(string column, DateTimeOffset since)
    {
        var safe = column switch
        {
            "cpu_pct" or "ram_pct" or "gpu_pct" or "disk_pct"
                or "cpu_temp" or "gpu_temp" or "ssd_temp" or "fan_rpm"
                or "v12" or "v5" or "v33" => column,
            _ => "cpu_pct",
        };
        using var cmd = _db.CreateCommand();
        cmd.CommandText = $"SELECT ts, {safe} FROM samples WHERE ts >= $since ORDER BY ts";
        cmd.Parameters.AddWithValue("$since", since.ToUnixTimeSeconds());
        var list = new List<(DateTime, double?)>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var at = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(r.GetDouble(0))).LocalDateTime;
            double? v = r.IsDBNull(1) ? null : r.GetDouble(1);
            list.Add((at, v));
        }
        return list;
    }

    public int Count()
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM samples";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void Bind(SqliteCommand cmd, string name, double? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value is null ? DBNull.Value : value.Value;
        cmd.Parameters.Add(p);
    }

    public void Dispose() => _db.Dispose();
}
