import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/pulso_colors.dart';
import '../../core/theme/pulso_wordmark.dart';
import '../../core/version.dart';

/// Tela inicial. O app abre aqui — ler QR é uma ação que o usuário escolhe,
/// não o primeiro passo obrigatório.
class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: PulsoColors.voidBg,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(28, 24, 28, 20),
          child: Column(
            children: [
              const Spacer(flex: 3),
              const PulsoWordmark(size: 40),
              const SizedBox(height: 14),
              Text(
                'O segundo monitor no seu bolso.',
                textAlign: TextAlign.center,
                style: GoogleFonts.rajdhani(
                  fontSize: 17,
                  color: PulsoColors.muted,
                  height: 1.4,
                ),
              ),
              const Spacer(flex: 4),
              _ScanButton(onTap: () => context.go('/scan')),
              const SizedBox(height: 14),
              Text(
                'Mesma Wi‑Fi do PC. Sem conta, sem nuvem.',
                textAlign: TextAlign.center,
                style: GoogleFonts.rajdhani(fontSize: 13, color: PulsoColors.muted),
              ),
              const Spacer(flex: 3),
              Text(
                'PULSO v$pulsoVersion',
                style: GoogleFonts.rajdhani(
                  fontSize: 12,
                  letterSpacing: 1.5,
                  color: PulsoColors.muted.withValues(alpha: 0.6),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ScanButton extends StatelessWidget {
  const _ScanButton({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(14),
        child: Ink(
          width: double.infinity,
          padding: const EdgeInsets.symmetric(vertical: 18),
          decoration: BoxDecoration(
            color: PulsoColors.cpu,
            borderRadius: BorderRadius.circular(14),
            boxShadow: [
              BoxShadow(
                color: PulsoColors.cpu.withValues(alpha: 0.45),
                blurRadius: 24,
                spreadRadius: -2,
              ),
            ],
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.qr_code_scanner_rounded, color: PulsoColors.voidBg, size: 22),
              const SizedBox(width: 10),
              Text(
                'LER QR CODE',
                style: GoogleFonts.chakraPetch(
                  fontSize: 15,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 1.5,
                  color: PulsoColors.voidBg,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
