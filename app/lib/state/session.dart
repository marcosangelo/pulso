import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/protocol/pairing_link.dart';
import '../core/protocol/telemetry.dart';
import '../data/pairing_log.dart';
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
  Timer? _watchdog;

  @override
  SessionState build() {
    ref.onDispose(() {
      _watchdog?.cancel();
      unawaited(_sub?.cancel());
    });
    return const SessionState.idle();
  }

  Future<void> connect(PairingLink link) async {
    await _sub?.cancel();
    _watchdog?.cancel();
    PairingLog.add('session connecting ${link.liveWs}');
    state = SessionState(phase: LinkPhase.connecting, link: link);
    _watchdog = Timer(const Duration(seconds: 10), () {
      if (state.phase == LinkPhase.connecting) {
        PairingLog.add('watchdog 10s ainda connecting — ${PairingLog.location}');
        state = SessionState(
          phase: LinkPhase.error,
          link: link,
          error: _timeout(link),
        );
      }
    });
    try {
      _sub = ref.read(telemetryGatewayProvider).watch(link).listen(
        (telemetry) {
          _watchdog?.cancel();
          PairingLog.add('live cpu=${telemetry.cpu.load}');
          state = SessionState(
            phase: LinkPhase.live,
            link: link,
            telemetry: telemetry,
          );
        },
        onError: (Object err) {
          _watchdog?.cancel();
          PairingLog.add('listen onError $err');
          state = SessionState(
            phase: LinkPhase.error,
            link: link,
            error: _friendly(err, link),
          );
        },
        onDone: () {
          PairingLog.add('listen onDone phase=${state.phase.name}');
          if (state.phase == LinkPhase.connecting || state.phase == LinkPhase.live) {
            _watchdog?.cancel();
            state = SessionState(
              phase: LinkPhase.error,
              link: link,
              error: state.phase == LinkPhase.connecting
                  ? _timeout(link)
                  : 'O PC fechou a conexão.',
            );
          }
        },
      );
    } catch (err) {
      _watchdog?.cancel();
      PairingLog.add('connect throw $err');
      state = SessionState(
        phase: LinkPhase.error,
        link: link,
        error: _friendly(err, link),
      );
    }
  }

  Future<void> disconnect() async {
    _watchdog?.cancel();
    await _sub?.cancel();
    _sub = null;
    PairingLog.add('disconnect');
    state = const SessionState.idle();
  }

  static String _timeout(PairingLink link) =>
      'Não ficou ao vivo em 10s.\n${link.liveWs}\nLog do app: ${PairingLog.location}\nNo PC: %LOCALAPPDATA%\\Pulso\\companion.log';

  static String _friendly(Object err, PairingLink link) {
    final text = err.toString();
    if (text.contains('TimeoutException') || text.contains('timed out')) {
      return _timeout(link);
    }
    if (text.contains('Failed host lookup') ||
        text.contains('SocketException') ||
        text.contains('Connection refused') ||
        text.contains('Connection failed')) {
      return 'Não achou o PC.\n${link.liveWs}\nMesma Wi‑Fi? Firewall?\nLog: ${PairingLog.location}';
    }
    if (text.contains('401') || text.contains('HttpException')) {
      return 'QR velho ou token inválido. Gere um código novo na aba Celular.\nLog: ${PairingLog.location}';
    }
    return '$text\n${link.liveWs}\nLog: ${PairingLog.location}';
  }
}
