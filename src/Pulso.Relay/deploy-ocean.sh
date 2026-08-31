#!/bin/bash
# Rode na droplet: bash deploy-ocean.sh
set -euo pipefail
BIND=http://0.0.0.0:8080
OUT=/opt/pulso-relay
SRC=$(cd "$(dirname "$0")" && pwd)

apt-get update -y
if ! command -v dotnet >/dev/null; then
  apt-get install -y dotnet-sdk-8.0
fi
apt-get install -y ufw
ufw allow 22/tcp
ufw allow 8080/tcp
ufw --force enable

dotnet publish "$SRC/Pulso.Relay.csproj" -c Release -o "$OUT"

cat >/etc/systemd/system/pulso-relay.service <<EOF
[Unit]
Description=Pulso Relay
After=network.target

[Service]
WorkingDirectory=$OUT
ExecStart=/usr/bin/dotnet $OUT/Pulso.Relay.dll
Environment=PULSO_RELAY_BIND=$BIND
Restart=always

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now pulso-relay
systemctl restart pulso-relay
curl -fsS http://127.0.0.1:8080/health
echo
echo "Aberto em ws://157.245.241.87:8080 — libere 8080 no firewall da DigitalOcean."
