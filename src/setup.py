#!/usr/bin/env python3
"""
Setup script for Automatic Edge Browser Web Searcher
Automatically creates a virtual environment, installs dependencies and Playwright browsers,
ensures admin rights, and then prints manual activation instructions based on your current shell.
"""
import subprocess
import sys
import os
import venv
from pathlib import Path
try:
    import psutil
except ImportError:
    print("❌ psutil not found. Please run 'pip install psutil' and rerun this script.")
    sys.exit(1)  # exit if psutil is missing

# Optional: colored output
try:
    from colorama import init, Fore
    init(autoreset=True)
except ImportError:
    Fore = Style = None

# --- Utility Functions ---
def run_command(command, description):
    """Run a command and handle errors."""
    print(f"\n🔧 {description}...")
    try:
        proc = subprocess.Popen(command, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        for line in proc.stdout:
            print(line, end='')
        proc.wait()
        if proc.returncode != 0:
            stderr = proc.stderr.read()
            raise subprocess.CalledProcessError(proc.returncode, command, stderr=stderr)
        print(f"✅ {description} completed successfully")
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ {description} failed: {e}")
        if e.stderr:
            print(f"Error: {e.stderr}")
        return False


def create_venv(venv_dir):
    """Create a virtual environment."""
    print(f"\n🔧 Creating virtual environment at {venv_dir}...")
    try:
        venv.create(venv_dir, with_pip=True)
        print(f"✅ Virtual environment created successfully")
        return True
    except Exception as e:
        print(f"❌ Failed to create virtual environment: {e}")
        return False


def get_executable(venv_dir, name):
    """Get a venv executable path (python or pip)."""
    if sys.platform == 'win32':
        return os.path.join(venv_dir, 'Scripts', name + '.exe')
    return os.path.join(venv_dir, 'bin', name)


def is_admin():
    """Return True if running with administrative privileges."""
    if os.name == 'nt':
        try:
            import ctypes
            return ctypes.windll.shell32.IsUserAnAdmin() != 0
        except Exception:
            return False
    return os.geteuid() == 0


def detect_shell():
    """Detect parent shell name via psutil."""
    try:
        parent = psutil.Process(os.getpid()).parent()
        return parent.name().lower()
    except Exception:
        return None


def print_activation_instructions(venv_dir):
    """Print manual activation instructions based on detected shell."""
    shell = detect_shell()
    print("\n🎉 Setup completed!")
    if shell in ('powershell.exe', 'pwsh.exe'):
        cmd = f". {venv_dir}/Scripts/Activate.ps1"
        note = "# Then run: python main.py"
    else:
        cmd = f"{venv_dir}\\Scripts\\activate.bat"
        note = "REM Then run: python main.py"

    # Colored output if available
    if Fore:
        print(Fore.GREEN + "To activate your virtual environment, run:")
        print(Fore.GREEN + cmd)
        print(Fore.YELLOW + note)
    else:
        print("To activate your virtual environment, run:")
        print(cmd)
        print(note)

# --- Main Setup ---
def main():
    print("🚀 Setting up Automatic Edge Browser Web Searcher...")

    if not is_admin():
        print("❌ This setup must be run as Administrator/root. Please restart with elevated privileges.")
        return False

    if sys.version_info < (3, 8):
        print("❌ Python 3.8+ required; current version:", sys.version)
        return False
    print(f"✅ Python {sys.version.split()[0]} detected")

    script_dir = Path(__file__).parent.resolve()
    venv_dir = script_dir / '.venv'

    if not create_venv(venv_dir):
        return False

    venv_pip = get_executable(venv_dir, 'pip')
    venv_python = get_executable(venv_dir, 'python')

    # Install dependencies
    req_file = script_dir / 'requirements.txt'
    if not run_command(f'"{venv_pip}" install -r "{req_file}"', "Installing Python dependencies"):
        return False

    # Install psutil (already imported but ensure latest)
    if not run_command(f'"{venv_pip}" install psutil', "Installing psutil for shell detection"):
        return False

    # Install Playwright browsers
    if not run_command(f'"{venv_python}" -m playwright install', "Installing Playwright browsers"):
        print("⚠️ Browser install failed; you may need to run 'playwright install' manually.")

    # Print manual activation instructions
    print_activation_instructions(venv_dir)
    return True

if __name__ == '__main__':
    success = main()
    if not success:
        sys.exit(1)