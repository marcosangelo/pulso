import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:wakelock_plus/wakelock_plus.dart';

import '../../core/theme/pulso_colors.dart';
import '../../state/session.dart';
import 'hud_landscape.dart';
import 'hud_portrait.dart';

class HudPage extends ConsumerStatefulWidget {
  const HudPage({super.key});

  @override
  ConsumerState<HudPage> createState() => _HudPageState();
}

class _HudPageState extends ConsumerState<HudPage> {
  @override
  void initState() {
    super.initState();
    WakelockPlus.enable();
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
  }

  @override
  void dispose() {
    WakelockPlus.disable();
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(sessionProvider);
    final data = session.telemetry;
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            _Chrome(
              live: session.isLive,
              host: session.link?.host ?? '',
              error: session.error,
              onClose: () => ref.read(sessionProvider.notifier).disconnect(),
            ),
            if (session.phase == LinkPhase.connecting)
              const Expanded(child: Center(child: CircularProgressIndicator()))
            else if (session.phase == LinkPhase.error)
              Expanded(
                child: Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Text(
                      session.error ?? 'Falha no link',
                      textAlign: TextAlign.center,
                      style: GoogleFonts.rajdhani(fontSize: 18, color: PulsoColors.hot),
                    ),
                  ),
                ),
              )
            else if (data != null)
              Expanded(
                child: OrientationBuilder(
                  builder: (context, orientation) {
                    if (orientation == Orientation.landscape) {
                      return HudLandscape(data: data);
                    }
                    return HudPortrait(data: data);
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _Chrome extends StatelessWidget {
  const _Chrome({
    required this.live,
    required this.host,
    required this.onClose,
    this.error,
  });

  final bool live;
  final String host;
  final String? error;
  final VoidCallback onClose;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 8, 4),
      child: Row(
        children: [
          Container(
            width: 9,
            height: 9,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: live ? PulsoColors.ok : PulsoColors.hot,
              boxShadow: [
                BoxShadow(
                  color: (live ? PulsoColors.ok : PulsoColors.hot).withValues(alpha: 0.7),
                  blurRadius: 8,
                ),
              ],
            ),
          ),
          const SizedBox(width: 10),
          Text(
            live ? 'PULSO LIVE' : 'PULSO',
            style: GoogleFonts.orbitron(
              fontSize: 13,
              fontWeight: FontWeight.w700,
              letterSpacing: 2,
              color: PulsoColors.cpu,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              host,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: GoogleFonts.rajdhani(color: PulsoColors.muted, fontSize: 14),
            ),
          ),
          IconButton(
            onPressed: onClose,
            icon: const Icon(Icons.close, color: PulsoColors.muted),
          ),
        ],
      ),
    );
  }
}
