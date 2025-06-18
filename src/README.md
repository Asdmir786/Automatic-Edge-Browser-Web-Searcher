# 🚀 Automatic Edge Browser Web Searcher (v1.6.0)

A simple Python tool to automate Bing searches in Microsoft Edge on Windows. Includes helper scripts for setup and unlocking locked profile files.

---

## 📋 Prerequisites

* **OS:** Windows 10 or higher  
* **Edge Browser:** Version 109.0.1518.78 or higher  
* **Python Libraries:** `psutil` is needed, install it with:

  ```powershell
  pip install psutil
  ```

* **Python:** Version 3.9+ (Python 3.13.4 most recommended)
  When installing Python:

  * Run the installer with **Administrator privileges**
  * Check **"Add python.exe to PATH"**
  * Click **"Disable path length limit"** to bypass 260-character MAX\_PATH

* **PowerShell (Run as Administrator):**

  After running the setup script, unblock PowerShell script execution:

  ```powershell
  Unblock-File -Path ".venv\Scripts\Activate.ps1"
  ```

---

## ⚙️ Installation & Setup

1. **Clone or download** this repository and open PowerShell **as Administrator**.

2. **Navigate into the project folder:**

   ```powershell
   cd C:\path\to\edge-searcher
   ```

3. **Run the setup script** to install dependencies and browsers:

   ```powershell
   python setup.py
   ```

   This will:

   * Install packages from `requirements.txt`
   * Run `playwright install` to fetch Chromium/Edge browsers

---

## ▶️ Running the Searcher

1. **Open PowerShell as Administrator**.

2. **Run the main automation script:**

   ```powershell
   python main.py
   ```

3. **Follow the prompts:**

   * Choose an Edge profile (or let it auto-select the default)
   * Choose direct or temporary profile copy
   * Enter the number of searches to perform

4. Sit back and watch human-like Edge browsing! 🌐🔍

> 💡 `queries.txt` includes **1000 ready-to-run unique queries**.

---

## 🛠️ Handling Locked Profile Files

If you encounter a locked file error (e.g., `WinError 32`), run:

```powershell
python process_killer.py History
```

* Replace `History` with any locked file name (e.g., `Cookies`, `Login Data`)
* Script will list and let you safely terminate locking processes

---

## 🎉 Enjoy!

Happy searching with your fully automated, human-like Edge Web Searcher!
