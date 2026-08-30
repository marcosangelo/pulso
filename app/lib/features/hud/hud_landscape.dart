import 'package:flutter/material.dart';

import '../../core/protocol/telemetry.dart';
import '../../core/theme/pulso_colors.dart';
import 'widgets/neon_gauge.dart';
import 'widgets/stat_chip.dart';

class HudLandscape extends StatelessWidget {
  const HudLandscape({super.key, required this.data});

  final Telemetry data;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Expanded(
          child: Row(
            children: [
              Expanded(
                child: NeonGauge(
                  label: 'CPU',
                  value: data.cpu.load,
                  color: PulsoColors.cpu,
                  caption: data.cpu.name,
                ),
              ),
              Expanded(
                flex: 3,
                child: NeonGauge(
                  label: 'GPU',
                  value: data.gpu.load,
                  color: PulsoColors.gpu,
                  caption: data.gpu.name,
                  hero: true,
                ),
              ),
              Expanded(
                child: NeonGauge(
                  label: 'RAM',
                  value: data.ram.load,
                  color: PulsoColors.ram,
                ),
              ),
            ],
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
          child: Row(
            children: [
              Expanded(
                child: StatChip(
                  label: 'CPU °C',
                  value: _n(data.cpu.temp, '°'),
                  alert: (data.cpu.temp ?? 0) >= 90,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: StatChip(
                  label: 'GPU °C',
                  value: _n(data.gpu.temp, '°'),
                  alert: (data.gpu.temp ?? 0) >= 90,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(child: StatChip(label: 'FAN', value: _n(data.fan.rpm, ''))),
              const SizedBox(width: 8),
              Expanded(child: StatChip(label: 'SSD', value: _n(data.disk.used, '%'))),
              const SizedBox(width: 8),
              Expanded(
                child: StatChip(
                  label: '12V',
                  value: _n(data.rails.v12, 'V', digits: 2),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  static String _n(double? v, String unit, {int digits = 0}) {
    if (v == null) return '—';
    return '${v.toStringAsFixed(digits)}$unit';
  }
}
