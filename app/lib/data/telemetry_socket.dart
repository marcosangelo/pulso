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

/// Isola o transporte. Trocar WS por relay na nuvem não mexe no HUD.
class TelemetryGateway {
  Stream<Telemetry> watch(PairingLink link) async* {
    PairingLog.add('connect ${link.liveWs}');
    final channel = WebSocketChannel.connect(link.liveWs);
    try {
      await channel.ready.timeout(const Duration(seconds: 8));
      PairingLog.add('websocket ready');
      await for (final event in channel.stream) {
        final raw = event is String ? event : utf8.decode(event as List<int>);
        PairingLog.add('frame ${raw.length}b');
        final json = jsonDecode(raw);
        if (json is! Map<String, dynamic>) {
          throw const FormatException('telemetry inválida');
        }
        yield Telemetry.fromJson(json);
      }
      PairingLog.add('stream done');
    } catch (err) {
      PairingLog.add('FAIL $err');
      rethrow;
    } finally {
      try {
        await channel.sink.close();
      } catch (_) {}
    }
  }
}
