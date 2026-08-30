import 'package:flutter_test/flutter_test.dart';
import 'package:pulso/core/protocol/pairing_link.dart';
import 'package:pulso/core/protocol/telemetry.dart';

void main() {
  test('parseia o QR v1 do desktop', () {
    const raw = 'pulso://link?v=1&h=192.168.0.10&p=8742&t=aabbccddeeff0011';
    final link = PairingLink.tryParse(raw);
    expect(link, isNotNull);
    expect(link!.host, '192.168.0.10');
    expect(link.port, 8742);
    expect(link.liveWs.toString(), contains('/v1/live'));
    expect(link.liveWs.queryParameters['t'], 'aabbccddeeff0011');
  });

  test('rejeita protocolo futuro', () {
    expect(PairingLink.tryParse('pulso://link?v=9&h=1.1.1.1&p=80&t=x'), isNull);
  });

  test('lê envelope de telemetria', () {
    final t = Telemetry.fromJson({
      'v': 1,
      'at': 1700000000000,
      'cpu': {'load': 41.2, 'temp': 62},
      'gpu': {'load': 88, 'name': 'RTX'},
      'ram': {'load': 70},
    });
    expect(t.cpu.load, 41.2);
    expect(t.gpu.name, 'RTX');
    expect(t.ram.load, 70);
  });
}
