import 'package:flutter/foundation.dart';

import 'pairing_log_persist_stub.dart'
    if (dart.library.io) 'pairing_log_persist_io.dart' as persist;

/// Log do pairing no celular. Console do `flutter run` + arquivo em [location].
class PairingLog {
  PairingLog._();

  static final List<String> lines = [];

  static String get location => persist.location;

  static void add(String message) {
    debugPrint('[pulso] $message');
    lines.add(message);
    if (lines.length > 80) lines.removeAt(0);
    persist.append(message);
  }
}
