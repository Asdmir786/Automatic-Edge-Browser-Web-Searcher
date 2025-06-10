# 🚀 Automatic Edge Browser Web Searcher (v2.1.1)

A simple Python tool to automate Bing searches in Microsoft Edge on Windows. Includes helper scripts for setup and unlocking locked profile files.

---

## 📋 Prerequisites

* **OS:** Windows 10 or higher
* **Edge Browser:** Version 109.0.1518.78 or higher
* **Python Libraries:** psutil is needed, you can install it with:

  ```bash
  pip install psutil
  ```
* **Python:** Version 3.9+ (Python 3.13.4 most recommended). When installing Python:

  * Run the installer with **Administrator privileges** (to configure `py.exe`).
  * Check **"Add python.exe to PATH"**.
  * Click **"Disable path length limit"** to bypass the 260-character MAX\_PATH limitation.
* **PowerShell:** Run as Administrator for full functionality

---

## ⚙️ Installation & Setup

1. **Clone or download** this repository and open PowerShell *as Administrator*.
2. **Navigate** into the project folder:

   ```powershell
   cd C:\path\to\edge-searcher
   ```
3. **Run the setup script** to install any missing dependencies and Playwright browsers:

   ```powershell
   python setup.py
   ```

   * Installs Python packages from `requirements.txt` (if not done already).
   * Invokes `playwright install` to fetch Edge/Chromium browsers.

---

## ▶️ Running the Searcher

1. **Open PowerShell as Administrator** (required for profile copying).
2. **Launch** the main automation:

   ```powershell
   python main.py
   ```
3. **Follow the prompts**:

   * Select your Edge profile (or let it use the default).
   * Choose between a direct profile launch or a temporary copy.
   * Enter the number of searches to perform.
4. **Enjoy** human-like automated searches in Edge! 🌐🔍

> **Tip:** `queries.txt` already contains **700 unique queries**, ready to go.

---

## 🛠️ Handling Locked Profile Files

If you ever see a file-lock error (e.g. “WinError 32”), use the helper script:

```powershell
python process_killer.py History
```

* Pass any part of the locked filename (e.g., `Cookies`, `History`).
* It will auto-elevate, list locking processes, and let you terminate them safely.

---

## 🎉 Enjoy!

Happy searching with your fully automated, human-like Edge Web Searcher!