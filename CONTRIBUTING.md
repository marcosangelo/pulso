# Contribuindo com o Pulso

Obrigado por aparecer. O Pulso é um produto pequeno e honesto: o Windows lê o hardware, o celular mostra o HUD. Contribuições que respeitam esses limites são bem-vindas.

Antes de código grande, [abra uma issue](https://github.com/marcosangelo/pulso/issues/new/choose) e descreva a ideia. Evita retrabalho.

## O que encaixa

- Correções de UI, acessibilidade e textos
- Sensores que o LibreHardwareMonitor já expõe e o Pulso ainda não mapeia
- HUD Flutter (retrato/paisagem, pairing, protocolo v1)
- Documentação, traduções, testes
- CI e empacotamento

## O que não entra

- Driver de kernel, WinRing0 próprio ou “jailbreak” de sensor
- Voltímetro de fonte genérica (o Windows não entrega isso)
- Relay na nuvem sem discussão prévia (hoje o link é só LAN)
- Exploit, keylogger, captura de senha — mesmo “para teste”

## Como rodar

### Windows (`src/Pulso`)

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- `publicar.bat` ou `dotnet build src/Pulso/Pulso.csproj`

### Celular (`app`)

- Flutter 3.47+
- `cd app && flutter pub get && flutter analyze && flutter test`

PC e celular na **mesma Wi‑Fi**. Firewall: permitir o Pulso na rede privada, porta **8742**.

## Protocolo

O QR é `pulso://link?v=1&h=&p=&t=`. O JSON do WebSocket é o envelope **v1** (`TelemetryEnvelope`). Campo novo entra **opcional**. Só suba o `v` se for quebrar o app antigo.

## Pull request

1. Fork → branch curta (`fix/qr-contraste`, `feat/hud-fps`)
2. Um assunto por PR
3. `flutter analyze` / `flutter test` e, se mexer no desktop, compile o WPF
4. Descreva o **porquê** no PR, não só o diff
5. Screenshot se a mudança for visual

O [código de conduta](CODE_OF_CONDUCT.md) vale para issues, PRs e discussões.
