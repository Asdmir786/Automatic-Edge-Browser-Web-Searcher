#!/usr/bin/env python3
"""
Setup script for Automatic Edge Browser Web Searcher
Automatically installs dependencies and Playwright browsers
"""

import subprocess
import sys
from pathlib import Path

def run_command(command, description):
    """Run a command and handle errors."""
    print(f"\n🔧 {description}...")
    try:
        process = subprocess.Popen(command, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')
        for line in process.stdout:
            print(line, end='')
        process.wait()
        if process.returncode != 0:
            raise subprocess.CalledProcessError(process.returncode, command, output=process.stdout.read(), stderr=process.stderr.read())
        print(f"✅ {description} completed successfully")
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ {description} failed: {e}")
        print(f"Error output: {e.stderr}")
        return False

def main():
    """Main setup function."""
    print("🚀 Setting up Automatic Edge Browser Web Searcher...")
    
    # Check Python version
    if sys.version_info < (3, 8):
        print("❌ Python 3.8+ required. Current version:", sys.version)
        return False
    
    print(f"✅ Python {sys.version.split()[0]} detected")
    
    # Install requirements
    if not run_command("pip install -r requirements.txt", "Installing Python dependencies"):
        return False
    
    # Install Playwright browsers
    if not run_command("playwright install", "Installing Playwright browsers"):
        print("""⚠️  Browser installation failed. You may need to run 'playwright install' manually.
           This will install all necessary browsers for Playwright.""")
    
    print("\n🎉 Setup completed! You can now run: python main.py")
    return True

if __name__ == "__main__":
    success = main()
    if not success:
        sys.exit(1)