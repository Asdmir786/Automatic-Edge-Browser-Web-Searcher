# build_exe.py
"""Builds a standalone EdgeSearch.exe from src/main.py using PyInstaller.

Usage (from project root, with Python ≥3.12):
    python installer\build_exe.py

This script will:
1. Ensure PyInstaller is installed (auto-installs locally if missing).
2. Run `pyinstaller` with the right options to bundle Playwright browsers and
   resources into a single-folder dist\EdgeSearch directory.
3. Output the final executable at dist/EdgeSearch/EdgeSearch.exe.
4. Copy the python_checker.py helper into the same dist folder so NSIS/MSI can
   ship it alongside the EXE.
"""
from __future__ import annotations
import subprocess
import sys
import shutil
from pathlib import Path
import os

PROJECT_ROOT = Path(__file__).resolve().parents[1]
SRC_DIR = PROJECT_ROOT / "src"
MAIN_PY = SRC_DIR / "main.py"
DIST_DIR = PROJECT_ROOT / "dist" / "EdgeSearch"
CHECKER_SRC = Path(__file__).with_name("python_checker.py")

PYI_SPEC_NAME = "EdgeSearch.spec"


def ensure_pyinstaller() -> None:
    """Install PyInstaller if not present."""
    try:
        import PyInstaller  # type: ignore
    except ModuleNotFoundError:
        print("[build_exe] PyInstaller not found. Installing...")
        subprocess.check_call([sys.executable, "-m", "pip", "install", "--upgrade", "pyinstaller"], shell=False)


def run_pyinstaller() -> None:
    """Invoke PyInstaller with sensible defaults."""
    args = [
        sys.executable,
        "-m",
        "PyInstaller",
        "--name",
        "EdgeSearch",
        "--noconfirm",
        "--clean",
        "--onefile",  # produce a single-exe; change to dir if Playwright size issues
        "--add-data",
        f"{SRC_DIR}{os.pathsep}src",  # bundle src folder as data (for queries.txt etc.)
        str(MAIN_PY),
    ]
    print("[build_exe] Running PyInstaller...\n ", " ".join(args))
    subprocess.check_call(args, shell=False)


def copy_checker() -> None:
    """Ensure python_checker.py is placed next to the EXE for installer packaging."""
    # Final staging directory for installer assets
    exe_folder = PROJECT_ROOT / "build" / "bin"
    exe_folder.mkdir(parents=True, exist_ok=True)

    # Source PyInstaller output (single-file build ends up at ./dist/EdgeSearch.exe)
    src_exe = PROJECT_ROOT / "dist" / "EdgeSearch.exe"
    if not src_exe.exists():
        raise FileNotFoundError(src_exe)

    # Copy EdgeSearch.exe and python_checker.py into build/bin
    for src in (src_exe, CHECKER_SRC):
        dst = exe_folder / src.name
        print(f"[build_exe] Copying {src} → {dst}")
        shutil.copy2(src, dst)


if __name__ == "__main__":
    ensure_pyinstaller()
    run_pyinstaller()
    copy_checker()
    print("[build_exe] ✔ Build finished. Files staged in build\\bin for installer.")
