# Segurança

O Pulso **não** envia telemetria para a internet. O app no celular fala com o PC só na LAN, com um token no QR.

## Relatar uma falha

Não abra issue pública se o problema permitir acesso indevido ao hub local.

1. [GitHub Security Advisory](https://github.com/marcosangelo/pulso/security/advisories/new)
2. Descreva o cenário (mesma rede, token, firewall)

## Limites conhecidos

- Token no QR: quem está na mesma Wi‑Fi e lê o código entra no stream
- `http://` / `ws://` na LAN (cleartext) — de propósito, sem certificado local
- Sem isolamento entre aparelhos (hotel / AP isolation) o pairing não funciona

Isso é desenho, não omissão. Mudanças (HTTPS local, relay) passam por issue primeiro.
