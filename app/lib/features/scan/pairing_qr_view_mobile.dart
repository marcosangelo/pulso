import 'dart:io';

import 'package:camera/camera.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:google_mlkit_barcode_scanning/google_mlkit_barcode_scanning.dart';

import '../../core/theme/pulso_colors.dart';

class PairingQrView extends StatefulWidget {
  const PairingQrView({super.key, required this.onCode});

  final ValueChanged<String> onCode;

  @override
  State<PairingQrView> createState() => _PairingQrViewState();
}

class _PairingQrViewState extends State<PairingQrView> {
  CameraController? _camera;
  late final BarcodeScanner _scanner;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _scanner = BarcodeScanner(formats: [BarcodeFormat.qrCode]);
    _open();
  }

  Future<void> _open() async {
    try {
      final cameras = await availableCameras();
      if (cameras.isEmpty) {
        setState(() => _error = 'Nenhuma câmera neste aparelho.');
        return;
      }
      final back = cameras.firstWhere(
        (c) => c.lensDirection == CameraLensDirection.back,
        orElse: () => cameras.first,
      );
      final controller = CameraController(
        back,
        ResolutionPreset.medium,
        enableAudio: false,
        imageFormatGroup: Platform.isAndroid
            ? ImageFormatGroup.nv21
            : ImageFormatGroup.bgra8888,
      );
      await controller.initialize();
      await controller.startImageStream(_onFrame);
      if (!mounted) {
        await controller.dispose();
        return;
      }
      setState(() => _camera = controller);
    } catch (err) {
      if (mounted) setState(() => _error = 'Câmera indisponível. $err');
    }
  }

  Future<void> _onFrame(CameraImage image) async {
    if (_busy || _camera == null) return;
    _busy = true;
    try {
      final input = _toInputImage(image, _camera!);
      if (input == null) return;
      final codes = await _scanner.processImage(input);
      final raw = codes
          .map((c) => c.rawValue)
          .whereType<String>()
          .firstWhere((v) => v.isNotEmpty, orElse: () => '');
      if (raw.isNotEmpty && mounted) widget.onCode(raw);
    } catch (_) {
      // frame ruim não pode derrubar o scan
    } finally {
      _busy = false;
    }
  }

  @override
  void dispose() {
    _camera?.dispose();
    _scanner.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) {
      return ColoredBox(
        color: PulsoColors.panel,
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Text(
              _error!,
              textAlign: TextAlign.center,
              style: GoogleFonts.rajdhani(color: PulsoColors.hot, fontSize: 16),
            ),
          ),
        ),
      );
    }
    final cam = _camera;
    if (cam == null || !cam.value.isInitialized) {
      return const ColoredBox(
        color: PulsoColors.panel,
        child: Center(child: CircularProgressIndicator()),
      );
    }
    return CameraPreview(cam);
  }
}

InputImage? _toInputImage(CameraImage image, CameraController camera) {
  final rotation = InputImageRotationValue.fromRawValue(
        camera.description.sensorOrientation,
      ) ??
      InputImageRotation.rotation0deg;
  final format = InputImageFormatValue.fromRawValue(image.format.raw) ??
      (Platform.isAndroid ? InputImageFormat.nv21 : InputImageFormat.bgra8888);

  final WriteBuffer buffer = WriteBuffer();
  for (final plane in image.planes) {
    buffer.putUint8List(plane.bytes);
  }
  final bytes = buffer.done().buffer.asUint8List();
  return InputImage.fromBytes(
    bytes: bytes,
    metadata: InputImageMetadata(
      size: Size(image.width.toDouble(), image.height.toDouble()),
      rotation: rotation,
      format: format,
      bytesPerRow: image.planes.first.bytesPerRow,
    ),
  );
}
