import 'package:flutter_test/flutter_test.dart';
import 'package:pulso/core/protocol/pairing_link.dart';
import 'package:pulso/core/protocol/telemetry.dart';

void main() {
  test('parseia o QR v1 do desktop', () {
    const raw = 'pulso://link?v=1&h=192.168.0.10&p=8742&t=aabbccddeeff0011';
    final link = PairingLink.tryParse(raw);
    expect(link, isNotNull);
    expect(link!.lan!.host, '192.168.0.10');
    expect(link.lan!.port, 8742);
    expect(link.relay, isNull);
    expect(link.targets.single.via, 'lan');
    expect(link.liveWs.queryParameters['t'], 'aabbccddeeff0011');
  });

  test('rejeita protocolo futuro', () {
    expect(PairingLink.tryParse('pulso://link?v=9&h=1.1.1.1&p=80&t=x'), isNull);
  });

  test('legado só relay wss', () {
    const raw = 'pulso://link?v=1&h=pulso.example.com&p=443&t=aabbccddeeff0011&s=wss';
    final link = PairingLink.tryParse(raw)!;
    expect(link.lan, isNull);
    expect(link.relay!.scheme, 'wss');
    expect(link.relay!.host, 'pulso.example.com');
    expect(link.targets.single.via, 'ocean');
  });

  test('QR com LAN e Ocean — Wi‑Fi primeiro', () {
    const raw =
        'pulso://link?v=1&h=192.168.3.17&p=8742&t=aabbccddeeff0011&rh=pulso.example.com&rp=443&rs=wss';
    final link = PairingLink.tryParse(raw)!;
    expect(link.hasLan, isTrue);
    expect(link.hasRelay, isTrue);
    expect(link.targets.map((t) => t.via).toList(), ['lan', 'ocean']);
    expect(link.lan!.host, '192.168.3.17');
    expect(link.relay!.host, 'pulso.example.com');
    expect(link.relay!.scheme, 'wss');
  });

  test('emulador Android aponta para o host', () {
    const raw = 'pulso://link?v=1&h=10.0.2.2&p=8742&t=aabbccddeeff0011';
    final link = PairingLink.tryParse(raw)!;
    expect(link.lan!.host, '10.0.2.2');
    expect(link.liveWs.host, '10.0.2.2');
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
