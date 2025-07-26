"""python_checker.py

Exit codes:
0  -> Python version OK (>=3.12)
1  -> Python not found
2  -> Python version too old
"""
from __future__ import annotations
import shutil
import subprocess
import sys
import re
from pathlib import Path

MIN_VERSION = (3, 12)


def get_python_path() -> Path | None:
    """Return Path to python.exe on PATH or None."""
    exe = shutil.which("python")
    return Path(exe) if exe else None


def get_version(python_path: Path) -> tuple[int, int, int]:
    out = subprocess.check_output([str(python_path), "-V"], text=True).strip()
    match = re.search(r"Python (\d+)\.(\d+)\.(\d+)", out)
    if not match:
        return (0, 0, 0)
    return tuple(map(int, match.groups()))  # type: ignore


def main() -> None:
    python_path = get_python_path()
    if not python_path:
        print("Python not found on PATH. Please install Python 3.12 or newer and try again.")
        sys.exit(1)

    version = get_version(python_path)
    if version < MIN_VERSION:
        print(f"Python {version[0]}.{version[1]}.{version[2]} detected. Python 3.12 or newer is required.")
        sys.exit(2)

    print(f"Python {version[0]}.{version[1]}.{version[2]} detected. Version OK.")
    sys.exit(0)


if __name__ == "__main__":
    main()
