<p align="center">
  <img src="docs/banner.svg" alt="Pulso — o pulso da sua máquina" width="720">
</p>

<p align="center">
  <b>Dashboard Windows nativo</b> + <b>HUD gamer no celular</b>.<br>
  Sem conta. Sem nuvem. Um QR na mesma Wi‑Fi.
</p>

<p align="center">
  <a href="https://github.com/marcosangelo/pulso/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/marcosangelo/pulso/ci.yml?branch=main&style=for-the-badge&label=CI" alt="CI"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-3EC4FF?style=for-the-badge" alt="MIT"></a>
  <img src="https://img.shields.io/badge/Windows-C%23%20·%20WPF-00F0FF?style=for-the-badge&logo=windows&logoColor=white" alt="Windows">
  <img src="https://img.shields.io/badge/Android-Flutter%20·%20ML%20Kit-FF2BD6?style=for-the-badge&logo=flutter&logoColor=white" alt="Flutter">
</p>

<p align="center">
  <a href="#-por-que-existe">Por quê</a> ·
  <a href="#-como-usar">Usar</a> ·
  <a href="#-o-que-o-hardware-entrega">Hardware</a> ·
  <a href="#-arquitetura">Arquitetura</a> ·
  <a href="#-contribuir">Contribuir</a>
</p>

---

## Por que existe

Quem joga ou compila num **monitor só** não tem para onde olhar CPU, GPU e temp. Overlay em cima do jogo atrapalha. Um segundo PC é luxo.

O **Pulso** é o segundo painel: o programa no Windows lê o hardware (a mesma pilha do HWMonitor) e o celular vira um HUD escuro, legível a 50 cm.

> Fonte genérica **não** vira voltímetro. O trilho 12 V, quando aparece, é sensor da **placa**. O produto fala isso na cara — não vende milagre.

## Como usar

### Windows

| | |
|---|---|
| 1 | [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (uma vez) |
| 2 | `publicar.bat` → sai `dist/Pulso/` |
| 3 | `Abrir-Pulso.bat` **ou** `dist/Pulso/Pulso.exe` |
| Admin | `Abrir-Pulso-Admin.bat` — necessário para Super I/O, **não suficiente** se a placa/HVCI não expuser Fan/Voltage |

Zip **a pasta inteira** `dist/Pulso` para mandar a alguém. Self-contained: o amigo não precisa ter .NET.

Histórico local: `%LOCALAPPDATA%\Pulso\history.db` (30 dias).

O Defender às vezes avisa em `.exe` novo. É o empacotador.

### Celular

Mesma Wi‑Fi. No PC, aba **Celular**. No aparelho:

```bash
cd app
flutter pub get
flutter run
```

Firewall do Windows: Pulso na rede **privada**, porta **8742**. Hotel com isolamento entre aparelhos não emparelha.

Guia do app: [`app/README.md`](app/README.md).

## O que o hardware entrega

| Card | Fonte | Quando some |
|------|--------|-------------|
| CPU · RAM · GPU | LibreHardwareMonitor | Quase nunca |
| Temp CPU (package) | MSR / DTS do processador | Quase nunca (não precisa de Super I/O) |
| Fans · 12 V · temp da placa | Super I/O / LPC + WinRing0 | HVCI/Integridade da memória, chip escondido, fan só Molex |
| Temp GPU | Driver NVIDIA / AMD | Sem driver |
| Disco C: | Windows | Quase nunca |
| 12 V · 5 V · 3.3 V | Sensor da **placa**, não da PSU | Fonte genérica não fala com o Windows |

Amostra na tela a cada **1 s**. Grava SQLite a cada **5 s**.

Abas no desktop: **Ao vivo** · **Histórico** · **Sensores** · **Celular** · **Sobre**.

## Arquitetura

```mermaid
flowchart LR
  subgraph pc [Windows]
    LHM[LibreHardwareMonitor]
    Tray[Hub na bandeja]
    LHM --> Tray
  end
  subgraph lan [Mesma Wi-Fi]
    Hub[":8742"]
  end
  subgraph cloud [DigitalOcean opcional]
    Relay[Pulso.Relay]
  end
  subgraph phone [Celular]
    HUD[HUD Flutter]
  end
  Tray --> Hub --> HUD
  Tray -->|"saída /v1/up"| Relay --> HUD
```

| Camada | Onde | Papel |
|--------|------|--------|
| Sensores | `src/Pulso/Hardware` | LHM: CPU, GPU, RAM, fans, trilhos |
| Histórico | `src/Pulso/Data` | SQLite local |
| Pairing | `src/Pulso/Link` | QR com LAN + Ocean; o app tenta Wi‑Fi primeiro |
| Bandeja | `src/Pulso/Shell` | Fecha o painel, o hub continua; autostart |
| Relay | `src/Pulso.Relay` | Cano na DigitalOcean — não lê sensor |
| Contrato | envelope JSON **v1** | Campos novos entram opcionais |
| HUD | `app/lib/features/hud` | Retrato e paisagem |

```
Pulso/
├── src/Pulso        painel + hub (bandeja)
├── src/Pulso.Relay  cano opcional (DigitalOcean)
├── app              HUD Flutter
├── docs
├── publicar.bat
└── Abrir-Pulso*.bat
```

Fecha o **X** da janela: o ícone fica na bandeja e a 8742 segue no ar. **Sair** no ícone desliga o hub. Marque **Abrir com o Windows** na aba Celular.

Relay: [`src/Pulso.Relay/README.md`](src/Pulso.Relay/README.md). No painel, cole `wss://seu.dominio` e gere o QR com esse host.

`dist/` é gerado. Não vai no git.

## Stack

| Desktop | App |
|---------|-----|
| .NET 8 · WPF | Flutter 3.47 · Dart 3.13 |
| LibreHardwareMonitor 0.9.6 | Riverpod 3 · go_router |
| Hub TCP 8742 + bandeja | Câmera + [ML Kit](https://developers.google.com/ml-kit) |
| SQLite | Material 3, tema neon |

## Contribuir

Leia **[`CONTRIBUTING.md`](CONTRIBUTING.md)** — o que entra, o que não entra, e como abrir PR.

- Issues: [bug](https://github.com/marcosangelo/pulso/issues/new?template=bug.yml) · [ideia](https://github.com/marcosangelo/pulso/issues/new?template=feature.yml)
- Conduta: [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md)
- Falha de segurança: [`SECURITY.md`](SECURITY.md)

PRs pequenos, um assunto, teste descrito. Protocolo v1 não quebra sem issue.

## Licença

[MIT](LICENSE) © 2026 [Marcos Angelo](https://github.com/marcosangelo)

---

<p align="center">
  <sub>Feito para quem olha o PC com um olho só. O outro fica no jogo.</sub>
</p>
