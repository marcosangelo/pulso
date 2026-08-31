import 'package:flutter/material.dart';

/// Tokens do HUD. Mesma paleta do dashboard Windows (tema Cyberpunk Neon):
/// CPU ciano, GPU magenta, RAM âmbar — lê de longe.
abstract final class PulsoColors {
  static const voidBg = Color(0xFF0B1020);
  static const panel = Color(0xFF141A2E);
  static const panelEdge = Color(0xFF2A3555);
  static const text = Color(0xFFE8EDF7);
  static const muted = Color(0xFFA8B0C4);
  static const cpu = Color(0xFF3EC4FF);
  static const gpu = Color(0xFFE64FD9);
  static const ram = Color(0xFFF5C14A);
  static const ok = Color(0xFF3DD68C);
  static const hot = Color(0xFFFF6B6B);
  static const warn = Color(0xFFF5C14A);
}
