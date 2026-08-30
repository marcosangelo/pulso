/// Contrato do QR gerado pelo Pulso desktop: `pulso://link?v=1&h=&p=&t=`
final class PairingLink {
  const PairingLink({
    required this.host,
    required this.port,
    required this.token,
    this.protocol = 1,
  });

  final String host;
  final int port;
  final String token;
  final int protocol;

  Uri get liveWs => Uri(
        scheme: 'ws',
        host: host,
        port: port,
        path: '/v1/live',
        queryParameters: {'t': token},
      );

  static PairingLink? tryParse(String raw) {
    final text = raw.trim();
    if (text.isEmpty) return null;
    final uri = Uri.tryParse(text);
    if (uri == null) return null;
    if (uri.scheme != 'pulso' || uri.host != 'link') return null;
    final host = uri.queryParameters['h'];
    final token = uri.queryParameters['t'];
    final port = int.tryParse(uri.queryParameters['p'] ?? '');
    final v = int.tryParse(uri.queryParameters['v'] ?? '1') ?? 1;
    if (host == null || host.isEmpty || token == null || token.isEmpty) return null;
    if (port == null || port < 1 || port > 65535) return null;
    if (v != 1) return null;
    return PairingLink(host: host, port: port, token: token, protocol: v);
  }
}
