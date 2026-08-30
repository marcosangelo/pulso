import 'dart:io';

final _file = File('${Directory.systemTemp.path}/pulso-pairing.log');

void append(String message) {
  try {
    _file.writeAsStringSync(
      '${DateTime.now().toIso8601String()} $message\n',
      mode: FileMode.append,
      flush: true,
    );
  } catch (_) {}
}

String get location => _file.path;
