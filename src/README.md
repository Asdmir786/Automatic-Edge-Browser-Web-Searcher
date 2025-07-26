# 🚀 Automatic Edge Browser Web Searcher (v1.6.0)

A simple Python tool to automate Bing searches in Microsoft Edge on Windows. Includes helper scripts for setup and unlocking locked profile files.

---

## 📋 Prerequisites

* **OS:** Windows 10 or higher  
* **Edge Browser:** Version 109.0.1518.78 or higher  
* **Python Libraries:** `psutil` is needed, install it with:

  ```cmd
  pip install psutil
  ```

* **Python:** Version 3.9+ (Python 3.13.4 most recommended)
  When installing Python:

  * Run the installer with **Administrator privileges**
  * Check **"Add python.exe to PATH"**
  * Click **"Disable path length limit"** to bypass 260-character MAX_PATH

* **Command Prompt (Run as Administrator):**

  **Note:** Use Command Prompt instead of PowerShell. PowerShell is not recommended due to execution policy restrictions and compatibility issues.

---

## ⚙️ Installation & Setup

1. **Clone or download** this repository and open Command Prompt **as Administrator**.

2. **Navigate into the project folder:**

   ```cmd
   cd C:\path\to\edge-searcher
   ```

3. **Run the setup script** to install dependencies and browsers:

   ```cmd
   python setup.py
   ```

   This will:

   * Install packages from `requirements.txt`
   * Run `playwright install` to fetch Chromium/Edge browsers

---

## ▶️ Running the Searcher

1. **Open Command Prompt as Administrator**.

2. **Run the main automation script:**

   ```cmd
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

If you encounter a locked file error (e.g., `WinError 32`), you can kill Edge processes with this command:

```cmd
taskkill /f /im msedge.exe
```

* This will forcefully terminate all Microsoft Edge processes
* Alternative: Use Task Manager to manually end Edge processes
* Wait a few seconds before retrying the script

---

## 🎉 Enjoy!

Happy searching with your fully automated, human-like Edge Web Searcher!
