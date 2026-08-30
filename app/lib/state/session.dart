import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/protocol/pairing_link.dart';
import '../core/protocol/telemetry.dart';
import '../data/telemetry_socket.dart';

enum LinkPhase { idle, connecting, live, error }

final class SessionState {
  const SessionState({
    required this.phase,
    this.link,
    this.telemetry,
    this.error,
  });

  const SessionState.idle() : this(phase: LinkPhase.idle);

  final LinkPhase phase;
  final PairingLink? link;
  final Telemetry? telemetry;
  final String? error;

  bool get isLive => phase == LinkPhase.live && telemetry != null;
}

final sessionProvider = NotifierProvider<SessionNotifier, SessionState>(
  SessionNotifier.new,
);

class SessionNotifier extends Notifier<SessionState> {
  StreamSubscription<Telemetry>? _sub;

  @override
  SessionState build() {
    ref.onDispose(() => unawaited(_sub?.cancel()));
    return const SessionState.idle();
  }

  Future<void> connect(PairingLink link) async {
    await _sub?.cancel();
    state = SessionState(phase: LinkPhase.connecting, link: link);
    try {
      _sub = ref.read(telemetryGatewayProvider).watch(link).listen(
        (telemetry) {
          state = SessionState(
            phase: LinkPhase.live,
            link: link,
            telemetry: telemetry,
          );
        },
        onError: (Object err) {
          state = SessionState(
            phase: LinkPhase.error,
            link: link,
            error: _friendly(err),
          );
        },
        onDone: () {
          if (state.phase == LinkPhase.live) {
            state = SessionState(
              phase: LinkPhase.error,
              link: link,
              error: 'O PC fechou a conexão.',
            );
          }
        },
      );
    } catch (err) {
      state = SessionState(
        phase: LinkPhase.error,
        link: link,
        error: _friendly(err),
      );
    }
  }

  Future<void> disconnect() async {
    await _sub?.cancel();
    _sub = null;
    state = const SessionState.idle();
  }

  static String _friendly(Object err) {
    final text = err.toString();
    if (text.contains('TimeoutException') || text.contains('timed out')) {
      return 'O PC não respondeu na 8742. Mesma Wi‑Fi (não 4G)? Firewall do Windows permitiu o Pulso? Emulador: escolha 10.0.2.2 no QR.';
    }
    if (text.contains('Failed host lookup') ||
        text.contains('SocketException') ||
        text.contains('Connection refused') ||
        text.contains('Connection failed')) {
      return 'Não achou o PC. Mesma Wi‑Fi? No emulador use 10.0.2.2. Firewall liberou o Pulso?';
    }
    if (text.contains('401') || text.contains('HttpException')) {
      return 'QR velho ou token inválido. Gere um código novo na aba Celular.';
    }
    return text;
  }
}
