import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/hud/hud_page.dart';
import '../../features/scan/scan_page.dart';
import '../../state/session.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final refresh = ValueNotifier<int>(0);
  ref.listen(sessionProvider, (_, next) => refresh.value++);
  ref.onDispose(refresh.dispose);

  return GoRouter(
    initialLocation: '/scan',
    refreshListenable: refresh,
    routes: [
      GoRoute(path: '/scan', builder: (context, state) => const ScanPage()),
      GoRoute(path: '/hud', builder: (context, state) => const HudPage()),
    ],
    redirect: (context, state) {
      final session = ref.read(sessionProvider);
      final atHud = state.matchedLocation == '/hud';
      if (session.isLive && !atHud) return '/hud';
      if (!session.isLive && atHud && session.phase == LinkPhase.idle) {
        return '/scan';
      }
      return null;
    },
  );
});
