import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/home/home_page.dart';
import '../../features/hud/hud_page.dart';
import '../../features/scan/scan_page.dart';
import '../../state/session.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final refresh = ValueNotifier<int>(0);
  ref.listen(sessionProvider, (_, next) => refresh.value++);
  ref.onDispose(refresh.dispose);

  return GoRouter(
    initialLocation: '/',
    refreshListenable: refresh,
    routes: [
      GoRoute(path: '/', builder: (context, state) => const HomePage()),
      GoRoute(path: '/scan', builder: (context, state) => const ScanPage()),
      GoRoute(path: '/hud', builder: (context, state) => const HudPage()),
    ],
    redirect: (context, state) {
      final session = ref.read(sessionProvider);
      final atHud = state.matchedLocation == '/hud';
      if (session.phase == LinkPhase.idle) {
        // Home e Scan são os dois pontos válidos em repouso — só tira da HUD.
        return atHud ? '/' : null;
      }
      return atHud ? null : '/hud';
    },
  );
});
