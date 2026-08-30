# Pulso — celular

HUD gamer: o celular vira segundo monitor. QR na aba **Celular** do Windows.

O produto inteiro (limites do hardware, Windows, contribuição): **[README na raiz](../README.md)**.

## Stack

Flutter **3.47** · Riverpod **3** · go_router · Material 3 · câmera oficial · [ML Kit](https://developers.google.com/ml-kit) (QR).

## Módulos

```
lib/
  core/protocol   contrato v1
  core/theme      neon (CPU ciano · GPU magenta · RAM âmbar)
  data/           WebSocket
  state/          sessão
  features/scan   câmera + cola de link
  features/hud    retrato e paisagem
```

## Rodar

Mesma Wi‑Fi. Firewall: Pulso, rede privada, **8742**.

```bash
flutter pub get
flutter analyze
flutter test
flutter run
```

Android SDK Command-line Tools: Android Studio → SDK Manager.

No Chrome: cole o `pulso://` (sem câmera).
