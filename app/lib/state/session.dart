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
    this.via,
  });

  const SessionState.idle() : this(phase: LinkPhase.idle);

  final LinkPhase phase;
  final PairingLink? link;
  final Telemetry? telemetry;
  final String? error;
  final String? via;

  bool get isLive => phase == LinkPhase.live && telemetry != null;

  String get viaLabel => switch (via) {
        'lan' => 'Wi‑Fi',
        'ocean' => 'Ocean',
        _ => '',
      };
}

final sessionProvider = NotifierProvider<SessionNotifier, SessionState>(
  SessionNotifier.new,
);

class SessionNotifier extends Notifier<SessionState> {
  StreamSubscription<LiveFrame>? _sub;
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
    PairingLog.add(
      'session LAN=${link.lan} Ocean=${link.relay}',
    );
    state = SessionState(phase: LinkPhase.connecting, link: link);
    final wait = link.hasLan && link.hasRelay
        ? const Duration(seconds: 16)
        : const Duration(seconds: 10);
    _watchdog = Timer(wait, () {
      if (state.phase == LinkPhase.connecting) {
        PairingLog.add('watchdog ainda connecting — ${PairingLog.location}');
        state = SessionState(
          phase: LinkPhase.error,
          link: link,
          error: _timeout(link),
        );
      }
    });
    try {
      _sub = ref.read(telemetryGatewayProvider).watch(link).listen(
        (frame) {
          _watchdog?.cancel();
          PairingLog.add('live via=${frame.via} cpu=${frame.telemetry.cpu.load}');
          state = SessionState(
            phase: LinkPhase.live,
            link: link,
            telemetry: frame.telemetry,
            via: frame.via,
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
              via: state.via,
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

  void disconnect() {
    _watchdog?.cancel();
    final sub = _sub;
    _sub = null;
    PairingLog.add('disconnect');
    state = const SessionState.idle();
    unawaited(sub?.cancel());
  }

  static String _timeout(PairingLink link) {
    final paths = [
      if (link.lan != null) 'Wi‑Fi ${link.lan}',
      if (link.relay != null) 'Ocean ${link.relay}',
    ].join('\n');
    return 'Não ficou ao vivo.\n$paths\nLog: ${PairingLog.location}';
  }

  static String _friendly(Object err, PairingLink link) {
    final text = err.toString();
    if (text.contains('TimeoutException') ||
        text.contains('timed out') ||
        text.contains('Failed host lookup') ||
        text.contains('SocketException') ||
        text.contains('Connection refused') ||
        text.contains('Connection failed')) {
      return _timeout(link);
    }
    if (text.contains('401') || text.contains('HttpException')) {
      return 'QR velho ou token inválido. Gere um código novo na aba Celular.\nLog: ${PairingLog.location}';
    }
    return '$text\nLog: ${PairingLog.location}';
  }
}
