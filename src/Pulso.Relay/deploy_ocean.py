"""Upload Pulso.Relay to the droplet and start systemd. Reads DROPLET_* from env file."""
from __future__ import annotations

import gzip
import shutil
import sys
from pathlib import Path

import paramiko

ROOT = Path(__file__).resolve().parents[2]
ENV_CANDIDATES = [
    ROOT / ".env.droplet.local",
]
LOCAL = ROOT / "dist" / "relay-linux"
REMOTE_DIR = "/opt/pulso-relay"
PUBKEY = Path.home() / ".ssh" / "id_ed25519.pub"


def load_env(path: Path) -> dict[str, str]:
    data: dict[str, str] = {}
    for raw in path.read_text(encoding="utf-8", errors="replace").splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        k, v = line.split("=", 1)
        data[k.strip()] = v.strip()
    return data


def env_path() -> Path:
    for p in ENV_CANDIDATES:
        if p.is_file():
            return p
    raise SystemExit("missing .env.droplet.local")


def run(ssh: paramiko.SSHClient, cmd: str, timeout: int = 120) -> str:
    _, stdout, stderr = ssh.exec_command(cmd, timeout=timeout)
    out = stdout.read().decode("utf-8", "replace")
    err = stderr.read().decode("utf-8", "replace")
    code = stdout.channel.recv_exit_status()
    if code != 0:
        raise RuntimeError(f"exit {code}\n$ {cmd}\n{out}{err}")
    return out


def main() -> None:
    cfg = load_env(env_path())
    host = cfg["DROPLET_HOST"]
    user = cfg.get("DROPLET_USER", "root")
    password = cfg["DROPLET_PASSWORD"]
    binary = LOCAL / "Pulso.Relay"
    if not binary.is_file():
        raise SystemExit(f"publish first: {binary}")

    gz = LOCAL / "Pulso.Relay.gz"
    print("gzip", binary.name, flush=True)
    with binary.open("rb") as src, gzip.open(gz, "wb", compresslevel=6) as dst:
        shutil.copyfileobj(src, dst)

    ssh = paramiko.SSHClient()
    ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    ssh.connect(
        host,
        username=user,
        password=password,
        timeout=20,
        banner_timeout=20,
        auth_timeout=20,
        allow_agent=False,
        look_for_keys=False,
    )

    print("connected", host, flush=True)
    print(run(ssh, "uname -a").strip(), flush=True)
    print(run(ssh, "free -h | head -2").strip(), flush=True)

    run(ssh, f"mkdir -p {REMOTE_DIR} /root/.ssh && chmod 700 /root/.ssh")
    if PUBKEY.is_file():
        key = PUBKEY.read_text(encoding="utf-8").strip()
        run(
            ssh,
            "touch /root/.ssh/authorized_keys && chmod 600 /root/.ssh/authorized_keys && "
            f"grep -qxF '{key}' /root/.ssh/authorized_keys || echo '{key}' >> /root/.ssh/authorized_keys",
        )
        print("ssh key installed", flush=True)

    sftp = ssh.open_sftp()
    remote_gz = f"{REMOTE_DIR}/Pulso.Relay.gz"
    remote_bin = f"{REMOTE_DIR}/Pulso.Relay"
    print(f"uploading {gz.name} ({gz.stat().st_size // 1024} KB)", flush=True)
    sftp.put(str(gz), remote_gz)
    sftp.close()
    print("unpacking on droplet", flush=True)
    run(ssh, f"gzip -df {remote_gz} && chmod +x {remote_bin}", timeout=180)

    unit = f"""[Unit]
Description=Pulso Relay
After=network.target

[Service]
WorkingDirectory={REMOTE_DIR}
ExecStart={remote_bin}
Environment=PULSO_RELAY_BIND=http://0.0.0.0:8080
Environment=ASPNETCORE_URLS=http://0.0.0.0:8080
Restart=always
RestartSec=3

[Install]
WantedBy=multi-user.target
"""
    run(ssh, f"cat > /etc/systemd/system/pulso-relay.service <<'EOF'\n{unit}EOF")

    # Keep Nginx/80 untouched. Open 8080 if ufw is on.
    ufw = run(ssh, "command -v ufw >/dev/null && ufw status || echo 'ufw-absent'")
    print(ufw.strip())
    if "Status: active" in ufw:
        run(ssh, "ufw allow 8080/tcp")
        print(run(ssh, "ufw status | grep 8080 || true").strip())

    run(ssh, "systemctl daemon-reload && systemctl enable pulso-relay && systemctl restart pulso-relay")
    print(run(ssh, "sleep 1; systemctl is-active pulso-relay").strip())
    print(run(ssh, "curl -fsS http://127.0.0.1:8080/health").strip())
    print(run(ssh, "ss -tlnp | grep -E ':8080|:80 ' || true").strip())
    ssh.close()
    print("deployed ws://%s:8080" % host)


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(e, file=sys.stderr)
        sys.exit(1)
