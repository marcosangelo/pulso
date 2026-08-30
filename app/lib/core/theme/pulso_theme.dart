import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'pulso_colors.dart';

abstract final class PulsoTheme {
  static ThemeData dark() {
    final base = ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      scaffoldBackgroundColor: PulsoColors.voidBg,
      colorScheme: const ColorScheme.dark(
        surface: PulsoColors.panel,
        primary: PulsoColors.cpu,
        secondary: PulsoColors.gpu,
        tertiary: PulsoColors.ram,
        onSurface: PulsoColors.text,
      ),
    );
    return base.copyWith(
      textTheme: GoogleFonts.rajdhaniTextTheme(base.textTheme).apply(
        bodyColor: PulsoColors.text,
        displayColor: PulsoColors.text,
      ),
    );
  }
}
