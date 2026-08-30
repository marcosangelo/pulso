/// Envelope v1. Campos novos no desktop entram como opcionais aqui.
final class Telemetry {
  const Telemetry({
    required this.at,
    required this.cpu,
    required this.gpu,
    required this.ram,
    required this.disk,
    required this.fan,
    required this.rails,
  });

  final DateTime at;
  final CpuTelemetry cpu;
  final GpuTelemetry gpu;
  final RamTelemetry ram;
  final DiskTelemetry disk;
  final FanTelemetry fan;
  final RailsTelemetry rails;

  factory Telemetry.fromJson(Map<String, dynamic> json) {
    final atMs = (json['at'] as num?)?.toInt();
    return Telemetry(
      at: atMs == null
          ? DateTime.now()
          : DateTime.fromMillisecondsSinceEpoch(atMs),
      cpu: CpuTelemetry.fromJson(asMap(json['cpu'])),
      gpu: GpuTelemetry.fromJson(asMap(json['gpu'])),
      ram: RamTelemetry.fromJson(asMap(json['ram'])),
      disk: DiskTelemetry.fromJson(asMap(json['disk'])),
      fan: FanTelemetry.fromJson(asMap(json['fan'])),
      rails: RailsTelemetry.fromJson(asMap(json['rails'])),
    );
  }

  static Map<String, dynamic> asMap(Object? value) =>
      value is Map<String, dynamic> ? value : const {};
}

final class CpuTelemetry {
  const CpuTelemetry({this.load, this.temp, this.clock, this.name});
  final double? load;
  final double? temp;
  final double? clock;
  final String? name;

  factory CpuTelemetry.fromJson(Map<String, dynamic> json) => CpuTelemetry(
        load: (json['load'] as num?)?.toDouble(),
        temp: (json['temp'] as num?)?.toDouble(),
        clock: (json['clock'] as num?)?.toDouble(),
        name: json['name'] as String?,
      );
}

final class GpuTelemetry {
  const GpuTelemetry({this.load, this.temp, this.name});
  final double? load;
  final double? temp;
  final String? name;

  factory GpuTelemetry.fromJson(Map<String, dynamic> json) => GpuTelemetry(
        load: (json['load'] as num?)?.toDouble(),
        temp: (json['temp'] as num?)?.toDouble(),
        name: json['name'] as String?,
      );
}

final class RamTelemetry {
  const RamTelemetry({this.load});
  final double? load;
  factory RamTelemetry.fromJson(Map<String, dynamic> json) =>
      RamTelemetry(load: (json['load'] as num?)?.toDouble());
}

final class DiskTelemetry {
  const DiskTelemetry({this.used, this.temp});
  final double? used;
  final double? temp;
  factory DiskTelemetry.fromJson(Map<String, dynamic> json) => DiskTelemetry(
        used: (json['used'] as num?)?.toDouble(),
        temp: (json['temp'] as num?)?.toDouble(),
      );
}

final class FanTelemetry {
  const FanTelemetry({this.rpm, this.name});
  final double? rpm;
  final String? name;
  factory FanTelemetry.fromJson(Map<String, dynamic> json) => FanTelemetry(
        rpm: (json['rpm'] as num?)?.toDouble(),
        name: json['name'] as String?,
      );
}

final class RailsTelemetry {
  const RailsTelemetry({this.v12, this.v5, this.v33});
  final double? v12;
  final double? v5;
  final double? v33;
  factory RailsTelemetry.fromJson(Map<String, dynamic> json) => RailsTelemetry(
        v12: (json['v12'] as num?)?.toDouble(),
        v5: (json['v5'] as num?)?.toDouble(),
        v33: (json['v33'] as num?)?.toDouble(),
      );
}
