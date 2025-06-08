# 🔍 Automatic Edge Browser Web Searcher (Python)

**Version 2.0.0** - Modern Python implementation with cross-platform support

## 📋 Overview

This Python application automates Microsoft Edge browser searches using Playwright. It loads search queries from a text file, removes duplicates, and performs random Bing searches with human-like behavior simulation.

### ✨ Key Features

- 🌐 **Cross-platform support** (Windows, macOS, Linux)
- 🔍 **Automated Edge browser control** via Playwright
- 👤 **Real Edge profile integration** with automatic copying
- 🎲 **Random query selection** without repetition
- 🤖 **Human-like typing simulation** with random delays
- 📊 **Progress tracking** with colored console output
- 🛡️ **Comprehensive error handling** and retry logic
- 📝 **Detailed logging** for debugging
- ⚙️ **Modern Python 3.13.4+ features**

## 🚀 Quick Start

### Prerequisites

- **Python 3.8+** (recommended: Python 3.13.4+)
- **Microsoft Edge browser** installed
- **Internet connection** for Playwright browser installation

### Installation

1. **Clone or download** this repository
2. **Navigate** to the project directory
3. **Run the setup script**:
   ```bash
   python setup.py
   ```

### Manual Installation

If the setup script doesn't work:

```bash
# Install dependencies
pip install -r requirements.txt

# Install Playwright Edge browser
playwright install msedge
```

### Usage

```bash
python main.py
```

## 📁 Project Structure

```
Automatic-Edge-Browser-Web-Searcher/
├── main.py              # Main application script
├── requirements.txt     # Python dependencies
├── setup.py            # Automated setup script
├── queries.txt         # Search queries (700+ included)
├── README.md           # This file
└── logs/               # Generated log files
```

## 🔧 Configuration

### Search Queries

Edit `queries.txt` to customize search queries:
- One query per line
- Comments start with `#`
- Duplicates are automatically removed

### Edge Profiles

The application automatically:
1. **Detects** available Edge profiles
2. **Lists** them for user selection
3. **Creates** temporary copies to avoid conflicts
4. **Uses** persistent browser context

## 🖥️ Platform Support

| Platform | Edge Profile Location | Status |
|----------|----------------------|--------|
| **Windows** | `%LOCALAPPDATA%\Microsoft\Edge\User Data` | ✅ Fully Supported |
| **macOS** | `~/Library/Application Support/Microsoft Edge` | ✅ Supported |
| **Linux** | `~/.config/microsoft-edge` | ✅ Supported |

## 🔍 How It Works

1. **System Detection**: Identifies OS and Edge installation
2. **Profile Discovery**: Finds available Edge user profiles
3. **Query Loading**: Reads and deduplicates search queries
4. **Profile Selection**: User chooses which profile to use
5. **Safe Copying**: Creates temporary profile copy
6. **Browser Launch**: Starts Edge with Playwright automation
7. **Search Execution**: Performs random searches with human-like delays
8. **Progress Tracking**: Shows real-time progress and results

## 🛠️ Advanced Usage

### Custom Browser Arguments

Modify the `args` list in `main.py` to customize browser behavior:

```python
args=[
    "--no-sandbox",
    "--disable-features=ImprovedCookieControls",
    "--disable-sync",
    # Add your custom arguments here
]
```

### Logging Configuration

Logs are automatically saved to:
- `log_debug.txt` (development mode)
- `log_release.txt` (production mode)

### Error Handling

The application includes robust error handling for:
- Network connectivity issues
- Browser launch failures
- Profile access problems
- Search execution errors

## 🔒 Security & Privacy

- **Profile Isolation**: Uses temporary profile copies
- **No Data Collection**: All searches are performed locally
- **Safe Automation**: Respects browser security features
- **Clean Cleanup**: Temporary files are managed automatically

## 🐛 Troubleshooting

### Common Issues

**"Playwright not installed"**
```bash
pip install playwright
playwright install msedge
```

**"No Edge profiles found"**
- Ensure Microsoft Edge is installed and has been run at least once
- Check if Edge profiles exist in the expected location

**"Browser launch failed"**
- Try running with administrator privileges
- Ensure Edge browser is not running
- Check antivirus software isn't blocking automation

**"Permission denied"**
- Close all Edge browser windows
- Run terminal/command prompt as administrator
- Check file permissions in the project directory

### Debug Mode

Run with Python's debug flag for verbose output:
```bash
python -O main.py  # Optimized mode
python main.py     # Debug mode (default)
```

## 📊 Performance

- **Startup Time**: ~2-5 seconds
- **Search Speed**: ~3-5 seconds per query
- **Memory Usage**: ~50-100MB
- **CPU Usage**: Low during searches

## 🔄 Migration from C# Version

This Python version maintains all core features from the original C# implementation:

### ✅ Retained Features
- Edge profile management
- Query deduplication
- Human-like search simulation
- Progress tracking
- Error handling and retries
- Logging system

### 🆕 New Features
- Cross-platform compatibility
- Modern Python async/await patterns
- Enhanced error messages
- Improved profile copying
- Better OS detection

### 🔄 Changes
- Removed Windows 10 restriction
- Simplified browser installation
- Enhanced cross-platform support
- Modern Python 3.13.4+ features

## 📝 License

This project is licensed under the terms specified in the LICENSE file.

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## 📞 Support

For issues and questions:
1. Check the troubleshooting section
2. Review the log files
3. Create an issue with detailed information

---

**Made with ❤️ using Python 3.13.4+ and Playwright**