import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:pulso/app.dart';

void main() {
  setUpAll(() {
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  testWidgets('LER QR CODE abre a tela de scan', (tester) async {
    await tester.pumpWidget(const ProviderScope(child: PulsoApp()));
    await tester.pump();

    expect(find.text('LER QR CODE'), findsOneWidget);

    await tester.tap(find.text('LER QR CODE'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.byTooltip('Voltar'), findsOneWidget);
    expect(find.byIcon(Icons.arrow_back_rounded), findsOneWidget);
  });
}
