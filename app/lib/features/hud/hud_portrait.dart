import 'package:flutter/material.dart';

import '../../core/protocol/telemetry.dart';
import '../../core/theme/pulso_colors.dart';
import 'widgets/neon_gauge.dart';
import 'widgets/stat_chip.dart';

class HudPortrait extends StatelessWidget {
  const HudPortrait({super.key, required this.data});

  final Telemetry data;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Expanded(
          flex: 5,
          child: NeonGauge(
            label: 'GPU',
            value: data.gpu.load,
            color: PulsoColors.gpu,
            caption: data.gpu.name,
            hero: true,
          ),
        ),
        Expanded(
          flex: 4,
          child: Row(
            children: [
              Expanded(
                child: NeonGauge(
                  label: 'CPU',
                  value: data.cpu.load,
                  color: PulsoColors.cpu,
                  caption: data.cpu.clock == null
                      ? data.cpu.name
                      : '${data.cpu.clock!.toStringAsFixed(0)} MHz',
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
          padding: const EdgeInsets.fromLTRB(12, 0, 12, 8),
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              StatChip(
                label: 'CPU °C',
                value: _n(data.cpu.temp, '°'),
                alert: (data.cpu.temp ?? 0) >= 90,
              ),
              StatChip(
                label: 'GPU °C',
                value: _n(data.gpu.temp, '°'),
                alert: (data.gpu.temp ?? 0) >= 90,
              ),
              StatChip(label: 'FAN', value: _n(data.fan.rpm, '')),
              StatChip(label: 'SSD', value: _n(data.disk.used, '%')),
              StatChip(label: '12V', value: _n(data.rails.v12, 'V', digits: 2)),
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
