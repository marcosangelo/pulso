import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'pulso_colors.dart';

/// O logotipo do Pulso: o traço de batimento + "PULSO" em Chakra Petch.
/// Mesmo desenho usado na barra superior do app Windows.
class PulsoWordmark extends StatelessWidget {
  const PulsoWordmark({super.key, this.size = 26});

  final double size;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        SizedBox(
          width: size * 1.5,
          height: size * 0.75,
          child: CustomPaint(painter: _PulseIconPainter(color: PulsoColors.cpu)),
        ),
        SizedBox(width: size * 0.4),
        Text(
          'PULSO',
          style: GoogleFonts.chakraPetch(
            fontSize: size,
            fontWeight: FontWeight.w600,
            letterSpacing: size * 0.12,
            color: PulsoColors.text,
          ),
        ),
      ],
    );
  }
}

class _PulseIconPainter extends CustomPainter {
  const _PulseIconPainter({required this.color});

  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final w = size.width;
    final h = size.height;
    // mesmo traço do zigue-zague usado no app Windows (viewBox 40x20)
    final pts = <Offset>[
      Offset(0, .5), Offset(.225, .5), Offset(.3, .2), Offset(.375, .8),
      Offset(.45, .1), Offset(.525, .9), Offset(.6, .5), Offset(1, .5),
    ].map((p) => Offset(p.dx * w, p.dy * h)).toList();

    final path = Path()..moveTo(pts.first.dx, pts.first.dy);
    for (final p in pts.skip(1)) {
      path.lineTo(p.dx, p.dy);
    }

    final glow = Paint()
      ..color = color.withValues(alpha: 0.55)
      ..style = PaintingStyle.stroke
      ..strokeWidth = h * 0.16
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round
      ..maskFilter = MaskFilter.blur(BlurStyle.normal, h * 0.22);
    canvas.drawPath(path, glow);

    final line = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = h * 0.12
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round;
    canvas.drawPath(path, line);
  }

  @override
  bool shouldRepaint(covariant _PulseIconPainter oldDelegate) => oldDelegate.color != color;
}
