#!/usr/bin/env python3
"""Manage a persistent local Matchmaker server process."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import sys
import time
from urllib.error import HTTPError, URLError
from urllib.request import urlopen


REPOSITORY_ROOT = Path(__file__).resolve().parents[4]
SERVER_PROJECT = (
    REPOSITORY_ROOT
    / "labs"
    / "agent-tuning-studio"
    / "src"
    / "Matchmaker.Server"
    / "Matchmaker.Server.csproj"
)
SERVER_PROJECT_RELATIVE = Path("labs/agent-tuning-studio/src/Matchmaker.Server/Matchmaker.Server.csproj")
LOG_PATH = REPOSITORY_ROOT / "labs" / "agent-tuning-studio" / "runs" / "matchmaker-server.log"


def process_path(port: int) -> Path:
    return LOG_PATH.with_name(f"matchmaker-server-{port}.json")


def reachable_host() -> str:
    """Return the host-side IPv4 address used by the shared browser."""
    probe = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        # UDP connect selects a route and local address without sending a packet.
        probe.connect(("192.0.2.1", 9))
        address = probe.getsockname()[0]
        return address if address and not address.startswith("127.") else "127.0.0.1"
    except OSError:
        return "127.0.0.1"
    finally:
        probe.close()


def server_status(url: str, timeout: float = 1.5) -> tuple[bool, str]:
    """Return whether the URL responds as this Matchmaker server."""
    try:
        with urlopen(f"{url.rstrip('/')}/api/matchmaker/status", timeout=timeout) as response:
            payload = json.load(response)
    except (HTTPError, URLError, TimeoutError, OSError, json.JSONDecodeError) as error:
        return False, str(error)

    if payload.get("phase") != "Matchmaker":
        return False, "the responding service is not the Matchmaker server"
    return True, str(payload.get("message", "Matchmaker server is responding."))


def wait_until_ready(url: str, process: subprocess.Popen[bytes], timeout: float) -> tuple[bool, str]:
    deadline = time.monotonic() + timeout
    last_error = "no response yet"
    while time.monotonic() < deadline:
        ready, detail = server_status(url)
        if ready:
            return True, detail
        last_error = detail
        if process.poll() is not None:
            break
        time.sleep(0.5)
    return False, last_error


def read_log_tail(path: Path, limit: int = 30) -> str:
    try:
        return "\n".join(path.read_text(encoding="utf-8", errors="replace").splitlines()[-limit:])
    except OSError:
        return "(server log is unavailable)"


def write_process_record(path: Path, process: subprocess.Popen[bytes], launch_url: str) -> None:
    record = {
        "pid": process.pid,
        "process_group": process.pid,
        "launch_url": launch_url,
        "project": str(SERVER_PROJECT),
    }
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(record) + "\n", encoding="utf-8")
    temporary.replace(path)


def managed_process_command(pid: int) -> str | None:
    """Read a PID command line so a stale/reused PID can never be signaled."""
    command_path = Path(f"/proc/{pid}/cmdline")
    try:
        return command_path.read_bytes().replace(b"\0", b" ").decode(errors="replace")
    except OSError:
        return None


def find_server_launch(launch_url: str) -> tuple[int, int] | None:
    """Find this repository's Matchmaker launch for safe adoption/restart."""
    if os.name != "posix":
        return None
    try:
        result = subprocess.run(
            ["ps", "-eo", "pid=,pgid=,args="],
            check=True,
            capture_output=True,
            text=True,
        )
    except (OSError, subprocess.CalledProcessError):
        return None

    identities = (str(SERVER_PROJECT), str(SERVER_PROJECT_RELATIVE))
    candidates: list[tuple[int, int, str]] = []
    for line in result.stdout.splitlines():
        columns = line.strip().split(maxsplit=2)
        if len(columns) != 3:
            continue
        try:
            pid, process_group = int(columns[0]), int(columns[1])
        except ValueError:
            continue
        command = columns[2]
        if launch_url in command and any(identity in command for identity in identities):
            candidates.append((pid, process_group, command))
    # Prefer the `dotnet run --project` supervisor because its process group
    # contains both the launcher and the actual Kestrel process.
    for pid, process_group, command in candidates:
        if "dotnet run" in command:
            return pid, process_group
    return candidates[0][:2] if candidates else None


def process_group_exists(process_group: int) -> bool:
    try:
        os.killpg(process_group, 0)
        return True
    except ProcessLookupError:
        return False
    except PermissionError:
        return True


def stop_managed_server(health_url: str, launch_url: str, port: int, timeout: float) -> tuple[bool, str]:
    record_path = process_path(port)
    ready, _ = server_status(health_url)
    try:
        record = json.loads(record_path.read_text(encoding="utf-8"))
        pid = int(record["pid"])
        process_group = int(record["process_group"])
    except FileNotFoundError:
        launch = find_server_launch(launch_url)
        if launch is None:
            if not ready:
                return True, "Matchmaker is already stopped"
            return False, "the responding server could not be identified as this repository's process"
        pid, process_group = launch
        record = {"pid": pid, "process_group": process_group, "launch_url": launch_url, "project": str(SERVER_PROJECT)}
    except (OSError, ValueError, KeyError, json.JSONDecodeError) as error:
        return False, f"the managed process record is invalid: {error}"

    recorded_launch_url = record.get("launch_url", record.get("url"))
    if not isinstance(recorded_launch_url, str) or record.get("project") != str(SERVER_PROJECT):
        return False, "the process record does not match this Matchmaker project and URL"
    command = managed_process_command(pid)
    project_identity = command is not None and (
        str(SERVER_PROJECT) in command or str(SERVER_PROJECT_RELATIVE) in command
    )
    if command is None or not project_identity or recorded_launch_url not in command:
        if not process_group_exists(process_group):
            record_path.unlink(missing_ok=True)
            return True, "removed a stale process record"
        return False, "the recorded PID no longer identifies the Matchmaker launch; refusing to stop it"
    try:
        if os.getpgid(pid) != process_group:
            return False, "the recorded process group changed; refusing to stop it"
    except ProcessLookupError:
        record_path.unlink(missing_ok=True)
        return True, "removed a stale process record"

    os.killpg(process_group, signal.SIGTERM)
    deadline = time.monotonic() + min(timeout, 10)
    while time.monotonic() < deadline:
        ready, _ = server_status(health_url, timeout=0.25)
        if not ready and not process_group_exists(process_group):
            record_path.unlink(missing_ok=True)
            return True, "stopped the helper-managed Matchmaker server"
        time.sleep(0.25)

    if process_group_exists(process_group):
        os.killpg(process_group, signal.SIGKILL)
        deadline = time.monotonic() + 5
        while process_group_exists(process_group) and time.monotonic() < deadline:
            time.sleep(0.1)
    ready, _ = server_status(health_url, timeout=0.25)
    if ready or process_group_exists(process_group):
        return False, "the Matchmaker process group did not stop cleanly"
    record_path.unlink(missing_ok=True)
    return True, "stopped the helper-managed Matchmaker server"


def ensure_server(health_url: str, launch_url: str, port: int, timeout: float) -> int:
    ready, detail = server_status(health_url)
    if ready:
        print(f"Matchmaker is already running. Browser URL: http://{reachable_host()}:{port}/")
        return 0

    LOG_PATH.parent.mkdir(parents=True, exist_ok=True)
    log = LOG_PATH.open("ab", buffering=0)
    command = [
        "dotnet",
        "run",
        "--project",
        str(SERVER_PROJECT),
        "--urls",
        launch_url,
    ]
    try:
        process = subprocess.Popen(
            command,
            cwd=REPOSITORY_ROOT,
            stdin=subprocess.DEVNULL,
            stdout=log,
            stderr=subprocess.STDOUT,
            start_new_session=True,
            close_fds=True,
        )
    except OSError as error:
        log.close()
        print(f"Could not start Matchmaker: {error}", file=sys.stderr)
        return 1
    log.close()
    write_process_record(process_path(port), process, launch_url)

    ready, detail = wait_until_ready(health_url, process, timeout)
    if ready:
        print(f"Started Matchmaker (pid {process.pid})")
        print(f"Browser URL: http://{reachable_host()}:{port}/")
        print(f"Server log: {LOG_PATH}")
        return 0

    print(f"Matchmaker did not become ready at {health_url}: {detail}", file=sys.stderr)
    try:
        os.killpg(process.pid, signal.SIGTERM)
    except (ProcessLookupError, PermissionError):
        pass
    process_path(int(url.rsplit(":", 1)[1])).unlink(missing_ok=True)
    print(f"Server log: {LOG_PATH}\n{read_log_tail(LOG_PATH)}", file=sys.stderr)
    return 1


def main() -> int:
    parser = argparse.ArgumentParser(description="Check, start, or restart the local Matchmaker server.")
    parser.add_argument("action", nargs="?", choices=("ensure", "status", "restart"), default="ensure")
    parser.add_argument("--port", type=int, default=5190, help="loopback port (default: 5190)")
    parser.add_argument("--wait-seconds", type=float, default=90, help="startup wait limit")
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("--port must be between 1 and 65535")
    if args.wait_seconds <= 0:
        parser.error("--wait-seconds must be positive")

    launch_url = f"http://{reachable_host()}:{args.port}"
    health_url = launch_url
    browser_url = f"http://{reachable_host()}:{args.port}/"
    ready, detail = server_status(health_url)
    if args.action == "status":
        if ready:
            print(f"Matchmaker is running: {detail}\nBrowser URL: {browser_url}")
        else:
            print(f"Matchmaker is not responding at {health_url}: {detail}")
        return 0
    if args.action == "restart":
        stopped, detail = stop_managed_server(health_url, launch_url, args.port, args.wait_seconds)
        if not stopped:
            print(f"Could not restart Matchmaker: {detail}", file=sys.stderr)
            return 1
        if detail.startswith("removed a stale"):
            print(detail)
        else:
            print("Stopped the existing helper-managed Matchmaker server.")
    return ensure_server(health_url, launch_url, args.port, args.wait_seconds)


if __name__ == "__main__":
    raise SystemExit(main())
