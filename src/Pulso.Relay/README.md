# Pulso Relay

Cano WebSocket. Não lê hardware. Droplet atual: **`157.245.241.87`** (NYC1, IP fixo, sem domínio).

- PC publica: `ws://157.245.241.87:8080/v1/up?t=TOKEN`
- App escuta: `ws://157.245.241.87:8080/v1/live?t=TOKEN`
- Saúde: `http://157.245.241.87:8080/health`

No painel (aba Celular): `ws://157.245.241.87:8080` → Aplicar. O QR leva LAN + esse IP; o app tenta Wi‑Fi primeiro.

## Subir na droplet (SSH uma vez)

```bash
ssh root@157.245.241.87
```

Depois, na máquina Ubuntu:

```bash
apt-get update
apt-get install -y dotnet-sdk-8.0 ufw
ufw allow 22
ufw allow 8080/tcp
ufw --force enable

# clone ou copie src/Pulso.Relay para /opt/pulso-relay-src
cd /opt/pulso-relay-src
dotnet publish -c Release -o /opt/pulso-relay
export PULSO_RELAY_BIND=http://0.0.0.0:8080
```

Serviço systemd — arquivo `/etc/systemd/system/pulso-relay.service`:

```ini
[Unit]
Description=Pulso Relay
After=network.target

[Service]
WorkingDirectory=/opt/pulso-relay
ExecStart=/usr/bin/dotnet /opt/pulso-relay/Pulso.Relay.dll
Environment=PULSO_RELAY_BIND=http://0.0.0.0:8080
Restart=always
User=root

[Install]
WantedBy=multi-user.target
```

```bash
systemctl daemon-reload
systemctl enable --now pulso-relay
curl -s http://127.0.0.1:8080/health
```

No painel da DigitalOcean: **Networking → Firewalls** — inbound TCP **8080** (e 22). Sem isso o IP não responde de fora.

Docker, se preferir:

```bash
cd /opt/pulso-relay-src
docker build -t pulso-relay .
docker run -d --restart unless-stopped -p 8080:8080 --name pulso-relay pulso-relay
```
