import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/pulso_colors.dart';

class PairingQrView extends StatelessWidget {
  const PairingQrView({super.key, required this.onCode});

  final ValueChanged<String> onCode;

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: PulsoColors.panel,
      child: Center(
        child: Text(
          'No navegador, cole o link.\nNo celular, a câmera lê o QR.',
          textAlign: TextAlign.center,
          style: GoogleFonts.rajdhani(
            color: PulsoColors.muted,
            fontSize: 16,
          ),
        ),
      ),
    );
  }
}
