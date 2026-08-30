import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/protocol/pairing_link.dart';
import '../../core/theme/pulso_colors.dart';
import '../../state/session.dart';
import 'pairing_qr_view.dart';

class ScanPage extends ConsumerStatefulWidget {
  const ScanPage({super.key});

  @override
  ConsumerState<ScanPage> createState() => _ScanPageState();
}

class _ScanPageState extends ConsumerState<ScanPage> {
  final _paste = TextEditingController();
  bool _armed = true;
  String? _hint;

  @override
  void dispose() {
    _paste.dispose();
    super.dispose();
  }

  Future<void> _apply(String raw) async {
    final link = PairingLink.tryParse(raw);
    if (link == null) {
      setState(() => _hint = 'QR inválido. Use o código da aba Celular do Pulso.');
      return;
    }
    if (!_armed) return;
    _armed = false;
    await ref.read(sessionProvider.notifier).connect(link);
    if (mounted && ref.read(sessionProvider).phase == LinkPhase.error) {
      _armed = true;
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(sessionProvider);
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 16, 20, 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'PULSO',
                style: GoogleFonts.orbitron(
                  fontSize: 28,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 6,
                  color: PulsoColors.cpu,
                ),
              ),
              Text(
                'Segundo monitor. Mesma Wi‑Fi.',
                style: GoogleFonts.rajdhani(
                  fontSize: 18,
                  color: PulsoColors.muted,
                ),
              ),
              const SizedBox(height: 16),
              Expanded(
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(18),
                  child: Stack(
                    fit: StackFit.expand,
                    children: [
                      PairingQrView(onCode: _apply),
                      IgnorePointer(
                        child: CustomPaint(painter: _ViewfinderPainter()),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 14),
              TextField(
                controller: _paste,
                style: GoogleFonts.rajdhani(color: PulsoColors.text, fontSize: 16),
                decoration: InputDecoration(
                  hintText: 'Ou cole o link pulso://',
                  hintStyle: GoogleFonts.rajdhani(color: PulsoColors.muted),
                  filled: true,
                  fillColor: PulsoColors.panel,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                    borderSide: const BorderSide(color: PulsoColors.panelEdge),
                  ),
                  suffixIcon: IconButton(
                    onPressed: () => _apply(_paste.text),
                    icon: const Icon(Icons.arrow_forward, color: PulsoColors.cpu),
                  ),
                ),
                onSubmitted: _apply,
              ),
              if (_hint != null || session.error != null)
                Padding(
                  padding: const EdgeInsets.only(top: 10),
                  child: Text(
                    session.error ?? _hint!,
                    style: GoogleFonts.rajdhani(color: PulsoColors.hot, fontSize: 14),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ViewfinderPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final rect = Rect.fromCenter(
      center: Offset(size.width / 2, size.height / 2),
      width: size.width * 0.72,
      height: size.width * 0.72,
    );
    final dim = Path()
      ..addRect(Rect.fromLTWH(0, 0, size.width, size.height))
      ..addRRect(RRect.fromRectAndRadius(rect, const Radius.circular(16)));
    canvas.drawPath(
      dim,
      Paint()
        ..color = const Color(0x9905070D)
        ..style = PaintingStyle.fill,
    );
    canvas.drawRRect(
      RRect.fromRectAndRadius(rect, const Radius.circular(16)),
      Paint()
        ..color = PulsoColors.cpu
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2,
    );
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
