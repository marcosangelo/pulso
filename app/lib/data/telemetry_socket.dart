import 'dart:async';
import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:web_socket_channel/web_socket_channel.dart';

import '../core/protocol/pairing_link.dart';
import '../core/protocol/telemetry.dart';
import 'pairing_log.dart';

final telemetryGatewayProvider = Provider<TelemetryGateway>((ref) {
  return TelemetryGateway();
});

class LiveFrame {
  const LiveFrame(this.via, this.telemetry);
  final String via;
  final Telemetry telemetry;
}

/// Isola o transporte. Wi‑Fi primeiro; Ocean só se a LAN não responder.
class TelemetryGateway {
  Stream<LiveFrame> watch(PairingLink link) async* {
    final targets = link.targets;
    if (targets.isEmpty) {
      throw const FormatException('QR sem destino');
    }

    Object? lastErr;
    for (var i = 0; i < targets.length; i++) {
      final t = targets[i];
      PairingLog.add('try ${t.via} ${t.uri} (${t.probe.inSeconds}s)');
      try {
        yield* _pipe(t.uri, t.via, t.probe);
        return;
      } catch (err) {
        lastErr = err;
        PairingLog.add('${t.via} FAIL $err');
        final hasNext = i < targets.length - 1;
        if (!hasNext) rethrow;
      }
    }
    throw lastErr ?? StateError('sem destino');
  }

  Stream<LiveFrame> _pipe(Uri uri, String via, Duration probe) async* {
    final channel = WebSocketChannel.connect(uri);
    try {
      await channel.ready.timeout(probe);
      PairingLog.add('ready $via');
      await for (final event in channel.stream) {
        final raw = event is String ? event : utf8.decode(event as List<int>);
        PairingLog.add('frame $via ${raw.length}b');
        final json = jsonDecode(raw);
        if (json is! Map<String, dynamic>) {
          throw const FormatException('telemetry inválida');
        }
        yield LiveFrame(via, Telemetry.fromJson(json));
      }
      PairingLog.add('stream done $via');
    } finally {
      try {
        await channel.sink.close();
      } catch (_) {}
    }
  }
}
