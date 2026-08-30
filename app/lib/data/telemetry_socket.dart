import 'dart:async';
import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:web_socket_channel/web_socket_channel.dart';

import '../core/protocol/pairing_link.dart';
import '../core/protocol/telemetry.dart';

final telemetryGatewayProvider = Provider<TelemetryGateway>((ref) {
  return TelemetryGateway();
});

/// Isola o transporte. Trocar WS por relay na nuvem não mexe no HUD.
class TelemetryGateway {
  Stream<Telemetry> watch(PairingLink link) {
    final channel = WebSocketChannel.connect(link.liveWs);
    return channel.stream.map((event) {
      final raw = event is String ? event : utf8.decode(event as List<int>);
      final json = jsonDecode(raw);
      if (json is! Map<String, dynamic>) {
        throw const FormatException('telemetry inválida');
      }
      return Telemetry.fromJson(json);
    });
  }
}
