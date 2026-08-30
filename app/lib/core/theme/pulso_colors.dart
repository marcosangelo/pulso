import 'package:flutter/material.dart';

/// Tokens do HUD. CPU ciano, GPU magenta, RAM âmbar — lê de longe.
abstract final class PulsoColors {
  static const voidBg = Color(0xFF05070D);
  static const panel = Color(0xFF0C1220);
  static const panelEdge = Color(0xFF1C2740);
  static const text = Color(0xFFE8EDF7);
  static const muted = Color(0xFF8B95AD);
  static const cpu = Color(0xFF00F0FF);
  static const gpu = Color(0xFFFF2BD6);
  static const ram = Color(0xFFFFC046);
  static const ok = Color(0xFF3DFFB0);
  static const hot = Color(0xFFFF4D6A);
  static const warn = Color(0xFFFFB020);
}
