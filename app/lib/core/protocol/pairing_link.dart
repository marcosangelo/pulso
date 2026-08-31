/// Contrato do QR: LAN em `h/p`, Ocean opcional em `rh/rp/rs`.
/// Legado: só `h/p` (Wi‑Fi) ou `h/p` + `s=wss` (só relay).
final class PairingLink {
  const PairingLink({
    required this.token,
    this.lan,
    this.relay,
    this.protocol = 1,
  });

  final String token;
  final Uri? lan;
  final Uri? relay;
  final int protocol;

  bool get hasLan => lan != null;
  bool get hasRelay => relay != null;

  /// Primeiro a Wi‑Fi local, depois a API — o app tenta nessa ordem.
  List<({Uri uri, String via, Duration probe})> get targets {
    final list = <({Uri uri, String via, Duration probe})>[];
    if (lan != null) {
      list.add((uri: lan!, via: 'lan', probe: const Duration(seconds: 3)));
    }
    if (relay != null) {
      list.add((uri: relay!, via: 'ocean', probe: const Duration(seconds: 8)));
    }
    return list;
  }

  /// Host para o chrome do HUD (LAN se existir).
  String get displayHost => lan?.host ?? relay?.host ?? '';

  /// Compatível com telas antigas que usavam um único liveWs.
  Uri get liveWs => lan ?? relay ?? Uri.parse('ws://invalid');

  static PairingLink? tryParse(String raw) {
    final text = raw.trim();
    if (text.isEmpty) return null;
    final uri = Uri.tryParse(text);
    if (uri == null) return null;
    if (uri.scheme != 'pulso' || uri.host != 'link') return null;
    final token = uri.queryParameters['t'];
    final v = int.tryParse(uri.queryParameters['v'] ?? '1') ?? 1;
    if (token == null || token.isEmpty) return null;
    if (v != 1) return null;

    final h = uri.queryParameters['h'];
    final p = int.tryParse(uri.queryParameters['p'] ?? '');
    final rh = uri.queryParameters['rh'];
    final rp = int.tryParse(uri.queryParameters['rp'] ?? '');
    final rs = uri.queryParameters['rs'];
    final legacySecure = uri.queryParameters['s'] == 'wss';

    Uri? lan;
    Uri? relay;

    if (rh != null && rh.isNotEmpty && rp != null && rp >= 1 && rp <= 65535) {
      if (h != null && h.isNotEmpty && p != null && p >= 1 && p <= 65535) {
        lan = _ws(secure: false, host: h, port: p, token: token);
      }
      relay = _ws(secure: rs != 'ws', host: rh, port: rp, token: token);
    } else if (h != null && h.isNotEmpty && p != null && p >= 1 && p <= 65535) {
      final u = _ws(secure: legacySecure, host: h, port: p, token: token);
      if (legacySecure) {
        relay = u;
      } else {
        lan = u;
      }
    } else {
      return null;
    }

    if (lan == null && relay == null) return null;
    return PairingLink(token: token, lan: lan, relay: relay, protocol: v);
  }

  static Uri _ws({
    required bool secure,
    required String host,
    required int port,
    required String token,
  }) =>
      Uri(
        scheme: secure ? 'wss' : 'ws',
        host: host,
        port: port,
        path: '/v1/live',
        queryParameters: {'t': token},
      );
}
