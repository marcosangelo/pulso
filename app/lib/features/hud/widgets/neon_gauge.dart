import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../core/theme/pulso_colors.dart';

class NeonGauge extends StatelessWidget {
  const NeonGauge({
    super.key,
    required this.label,
    required this.value,
    required this.color,
    this.unit = '%',
    this.caption,
    this.hero = false,
  });

  final String label;
  final double? value;
  final Color color;
  final String unit;
  final String? caption;
  final bool hero;

  @override
  Widget build(BuildContext context) {
    final v = value;
    return LayoutBuilder(
      builder: (context, box) {
        final side = math.min(box.maxWidth, box.maxHeight);
        final numberSize = hero ? side * 0.22 : side * 0.20;
        return CustomPaint(
          painter: _GaugePainter(value: v, color: color),
          child: Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  label,
                  style: GoogleFonts.rajdhani(
                    fontSize: hero ? 16 : 13,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 3,
                    color: color,
                  ),
                ),
                Text(
                  v == null ? '—' : v.toStringAsFixed(0),
                  style: GoogleFonts.orbitron(
                    fontSize: numberSize.clamp(28, 72),
                    fontWeight: FontWeight.w700,
                    color: PulsoColors.text,
                    height: 1.05,
                  ),
                ),
                Text(
                  unit,
                  style: GoogleFonts.rajdhani(
                    fontSize: 14,
                    color: PulsoColors.muted,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                if (caption != null && caption!.isNotEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 4, left: 12, right: 12),
                    child: Text(
                      caption!,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.rajdhani(
                        fontSize: 12,
                        color: PulsoColors.muted,
                      ),
                    ),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _GaugePainter extends CustomPainter {
  _GaugePainter({required this.value, required this.color});

  final double? value;
  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final side = math.min(size.width, size.height);
    final center = Offset(size.width / 2, size.height / 2);
    final radius = side * 0.42;
    const start = math.pi * 0.75;
    const sweep = math.pi * 1.5;
    final rect = Rect.fromCircle(center: center, radius: radius);

    final track = Paint()
      ..color = PulsoColors.panelEdge
      ..style = PaintingStyle.stroke
      ..strokeWidth = side * 0.055
      ..strokeCap = StrokeCap.round;
    canvas.drawArc(rect, start, sweep, false, track);

    if (value == null) return;
    final t = (value!.clamp(0, 100)) / 100;
    final glow = Paint()
      ..color = color.withValues(alpha: 0.35)
      ..style = PaintingStyle.stroke
      ..strokeWidth = side * 0.09
      ..strokeCap = StrokeCap.round
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 10);
    final arc = Paint()
      ..shader = SweepGradient(
        startAngle: start,
        endAngle: start + sweep,
        colors: [color.withValues(alpha: 0.4), color],
        transform: GradientRotation(start),
      ).createShader(rect)
      ..style = PaintingStyle.stroke
      ..strokeWidth = side * 0.055
      ..strokeCap = StrokeCap.round;
    canvas.drawArc(rect, start, sweep * t, false, glow);
    canvas.drawArc(rect, start, sweep * t, false, arc);
  }

  @override
  bool shouldRepaint(covariant _GaugePainter old) =>
      old.value != value || old.color != color;
}
