#!/usr/bin/env python3
"""
Automatic Edge Browser Web Searcher - Python Version
==================================================
Version: 2.1.0
Last updated: 2025-06-08

This Python application automates Microsoft Edge browser searches using Playwright.
It loads search queries from queries.txt, removes duplicates, and performs a specified
number of random Bing searches. The program simulates human-like typing and delays,
and provides progress feedback in the console. It enumerates real Edge user profiles
and allows the user to pick one for persistent context via Playwright.

This script will attempt to run with administrator privileges. If it encounters
WinError 32 (file locked by another process) during profile copying, you can use
the accompanying 'process_killer.py' script to identify and terminate the locking
process for the problematic file (e.g., Cookies).

Features:
- Cross-platform support (Windows, macOS, Linux)
- Modern Python 3.13.4 features
- Automatic Edge profile detection and copying
- Human-like search simulation
- Comprehensive error handling and logging
- Colored console output
"""

import asyncio
import logging
import os
import platform
import random
import shutil
import sys
import ctypes  # Added for admin check
from datetime import datetime
from pathlib import Path
from typing import List, Optional, Tuple
import psutil  # For process management

# --- Admin Check and Relaunch Logic (Windows Only) ---
def is_admin() -> bool:
    """Checks if the current script is running with admin privileges (Windows)."""
    if platform.system() == "Windows":
        try:
            return bool(ctypes.windll.shell32.IsUserAnAdmin())
        except:
            return False
    return True  # Assume admin or not applicable on non-Windows

def run_as_admin_if_not() -> None:
    """Checks for admin privileges on Windows and exits if not found."""
    if platform.system() == "Windows" and not is_admin():
        print(f"{Colors.RED}❌ This script requires administrator privileges to run.{Colors.RESET}")
        print(f"{Colors.YELLOW}   Please re-run this script as an administrator.{Colors.RESET}")
        sys.exit(1)  # Exit with an error code
# --- End Admin Check ---

try:
    from playwright.async_api import async_playwright, Browser, BrowserContext, Page
except ImportError:
    print("❌ Playwright not installed. Please run: pip install playwright")
    print("   Then run: playwright install")
    sys.exit(1)

# Windows-specific imports
if platform.system() == "Windows":
    try:
        import winreg
    except ImportError:
        winreg = None

# Color codes for cross-platform colored output
class Colors:
    """ANSI color codes for terminal output."""
    RED = '\033[91m'
    GREEN = '\033[92m'
    YELLOW = '\033[93m'
    BLUE = '\033[94m'
    MAGENTA = '\033[95m'
    CYAN = '\033[96m'
    WHITE = '\033[97m'
    BOLD = '\033[1m'
    RESET = '\033[0m'

class EdgeSearcher:
    """Main class for automating Edge browser searches."""
    
    def __init__(self):
        self.logger = self._setup_logging()
        self.queries: List[str] = []
        self.edge_profiles: List[Path] = []
        self.selected_profile: Optional[Path] = None
        self.use_direct_profile: bool = False
        
    def _setup_logging(self) -> logging.Logger:
        """Set up logging configuration."""
        log_filename = "log_debug.txt" if __debug__ else "log_release.txt"
        log_path = Path.cwd() / log_filename
        
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler(log_path, encoding='utf-8'),
                logging.StreamHandler()
            ]
        )
        
        logger = logging.getLogger(__name__)
        logger.info(f"Log file initialized at: {log_path}")
        return logger
    
    def _detect_os_version(self) -> Tuple[str, str]:
        """Detect operating system and version using modern Python methods."""
        system = platform.system()
        
        match system:
            case "Windows":
                # Use winreg for Windows version detection
                if winreg:
                    try:
                        with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, 
                                          r"SOFTWARE\Microsoft\Windows NT\CurrentVersion") as key:
                            product_name, _ = winreg.QueryValueEx(key, "ProductName")
                            return system, product_name
                    except (OSError, FileNotFoundError):
                        pass
                
                # Fallback to platform module
                return system, platform.release()
            
            case "Darwin":
                return "macOS", platform.mac_ver()[0]
            
            case _:
                return system, platform.release()
    
    def _get_edge_user_data_dir(self) -> Optional[Path]:
        """Get Edge user data directory based on the operating system."""
        system = platform.system()
        
        match system:
            case "Windows":
                return Path.home() / "AppData" / "Local" / "Microsoft" / "Edge" / "User Data"
            case "Darwin":
                return Path.home() / "Library" / "Application Support" / "Microsoft Edge"
            case "Linux":
                return Path.home() / ".config" / "microsoft-edge"
            case _:
                self.logger.warning(f"Unsupported operating system: {system}")
                return None
    
    def _find_edge_profiles(self) -> List[Path]:
        """Find available Edge user profiles."""
        edge_data_dir = self._get_edge_user_data_dir()
        
        if not edge_data_dir or not edge_data_dir.exists():
            self.logger.error(f"Edge user data directory not found: {edge_data_dir}")
            return []
        
        profiles = []
        for item in edge_data_dir.iterdir():
            if item.is_dir():
                name = item.name
                if name == "Default" or name.startswith("Profile "):
                    profiles.append(item)
        
        return sorted(profiles)
    
    def _load_queries(self) -> List[str]:
        """Load search queries from queries.txt file."""
        queries_file = Path.cwd() / "queries.txt"
        
        if not queries_file.exists():
            self.logger.error(f"Queries file not found: {queries_file}")
            return []
        
        try:
            with open(queries_file, 'r', encoding='utf-8') as f:
                queries = [
                    line.strip()           # 1. remove whitespace/newlines
                        .strip('",')       # 2. strip any leading/trailing " or , characters
                    for line in f
                    if line.strip()         # skip blank lines
                ]
            
            # Remove duplicates while preserving order
            unique_queries = list(dict.fromkeys(queries))
            
            self.logger.info(f"Loaded {len(queries)} total queries, {len(unique_queries)} unique")
            return unique_queries
            
        except Exception as e:
            self.logger.error(f"Error loading queries: {e}")
            return []
    
    def _copy_profile_safely(self, source_profile: Path) -> Path:
        """Create a safe copy of the Edge profile for automation."""
        if self.use_direct_profile:
            self.logger.info(f"Using profile directly: {source_profile}")
            return source_profile

        temp_profile = source_profile.parent / f"{source_profile.name}-temp"
        
        if temp_profile.exists():
            # Check if we can write to a test file in the temp profile to ensure it's not locked by a previous run
            try:
                test_file = temp_profile / "_can_write_test.tmp"
                with open(test_file, "w") as f_test:
                    f_test.write("test")
                os.remove(test_file)
                print(f"{Colors.GREEN}✓ Found existing and accessible temp profile: {temp_profile.name}{Colors.RESET}")
                return temp_profile
            except OSError as e:
                print(f"{Colors.YELLOW}⚠️ Existing temp profile '{temp_profile.name}' seems locked or inaccessible (Error: {e}). Attempting to recreate.{Colors.RESET}")
                try:
                    shutil.rmtree(temp_profile)
                    print(f"{Colors.GREEN}✓ Successfully removed old locked temp profile.{Colors.RESET}")
                except Exception as e_rm:
                    self.logger.error(f"Failed to remove existing locked temp profile '{temp_profile.name}': {e_rm}")
                    print(f"{Colors.RED}❌ Critical error: Could not remove locked temp profile '{temp_profile.name}'. Please remove it manually and restart.{Colors.RESET}")
                    # If process_killer.py is available, suggest using it.
                    killer_script_path = Path.cwd() / "process_killer.py"
                    if killer_script_path.exists():
                        print(f"{Colors.CYAN}💡 You can try using '{killer_script_path.name}' to find and kill processes locking files in this directory.{Colors.RESET}")
                    sys.exit(1)

        print(f"{Colors.YELLOW}📁 Creating temporary profile copy...{Colors.RESET}")
        
        # Create the destination directory
        temp_profile.mkdir(parents=True, exist_ok=True)
        
        # Track any errors that occur during copying
        copy_errors = []
        
        # Custom copy function that skips files that are locked
        def copy_file(src: Path, dst: Path) -> bool:
            try:
                shutil.copy2(src, dst)
                return True
            except OSError as e:
                # File is locked or otherwise inaccessible
                copy_errors.append((src, dst, str(e)))
                return False
        
        # Walk through the source directory and copy files
        for root, dirs, files in os.walk(source_profile):
            # Create relative path to create same structure in destination
            rel_path = os.path.relpath(root, source_profile)
            dst_dir = temp_profile / rel_path
            
            # Create destination directory
            dst_dir.mkdir(parents=True, exist_ok=True)
            
            # Copy each file
            for file in files:
                src_file = Path(root) / file
                dst_file = dst_dir / file
                
                # Skip lock files and temporary files
                if file.endswith('.lock') or file.endswith('.tmp'):
                    continue
                    
                copy_file(src_file, dst_file)
        
        if copy_errors:
            # Check for WinError 32 specifically
            for src, dst, error in copy_errors:
                if "WinError 32" in error or "being used by another process" in error:
                    self.logger.error(f"{Colors.RED}❌ File lock detected when copying profile {source_profile}.{Colors.RESET}")
                    self.logger.error(f"{Colors.RED}   Error: {error}{Colors.RESET}")
                    
                    # Find processes that might be locking files
                    locked_files = []
                    for proc in psutil.process_iter(['pid', 'name']):
                        try:
                            if hasattr(proc, 'open_files') and proc.open_files():
                                for f in proc.open_files():
                                    if str(src) == f.path:
                                        locked_files.append((src, proc.info['pid'], proc.info['name']))
                        except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
                            pass
                    
                    if locked_files:
                        self.logger.error(f"{Colors.RED}   Files locked by processes:{Colors.RESET}")
                        for file, pid, name in locked_files:
                            self.logger.error(f"{Colors.RED}     - {file} locked by PID={pid}, Name={name}{Colors.RESET}")
                    
                    self.logger.error(f"{Colors.YELLOW}   Consider using 'process_killer.py' to identify and terminate the locking process.{Colors.RESET}")
                    raise OSError(f"File copying failed due to locked files: {error}")
            
            print(f"{Colors.YELLOW}⚠️ Some files could not be copied, but will continue with partial profile.{Colors.RESET}")
        else:
            print(f"{Colors.GREEN}✓ Profile copied successfully to: {temp_profile.name}{Colors.RESET}")
            
        return temp_profile

    def _select_profile(self) -> Tuple[Optional[Path], bool, int]:
        """Allow user to select an Edge profile and get configuration."""
        self.edge_profiles = self._find_edge_profiles()
        
        if not self.edge_profiles:
            print(f"{Colors.RED}❌ No Edge profiles found.{Colors.RESET}")
            return None, False, 0
        
        print(f"{Colors.CYAN}Found {len(self.edge_profiles)} profiles:{Colors.RESET}")
        for i, profile in enumerate(self.edge_profiles, 1):
            print(f"  [{i}] {profile.name}")
        
        while True:
            try:
                choice = input(f"\n{Colors.YELLOW}Select profile number (default 1): {Colors.RESET}").strip()
                
                if not choice:
                    idx = 0
                else:
                    idx = int(choice) - 1
                
                if 0 <= idx < len(self.edge_profiles):
                    selected = self.edge_profiles[idx]
                    print(f"\n{Colors.GREEN}✓ Selected profile: {selected.name}{Colors.RESET}")

                    # Ask for usage method
                    while True:
                        choice = input(f"{Colors.CYAN}Use profile directly (D) or create temporary copy? (C) [D/c]: {Colors.RESET}").strip().lower()
                        if choice in ['d', '', 'direct']:
                            use_direct = True
                            break
                        elif choice in ['c', 'copy']:
                            use_direct = False
                            break
                        else:
                            print(f"{Colors.RED}❌ Invalid choice - please enter D/d or C/c{Colors.RESET}")

                    # Get search count
                    while True:
                        try:
                            input_val = input(f"{Colors.YELLOW}Enter number of searches to perform [5]: {Colors.RESET}")
                            count = int(input_val.strip()) if input_val.strip() else 5
                            if count > 0:
                                return selected, use_direct, count
                            print(f"{Colors.RED}❌ Must perform at least 1 search{Colors.RESET}")
                        except ValueError:
                            print(f"{Colors.RED}❌ Please enter a valid number{Colors.RESET}")
                else:
                    print(f"{Colors.RED}❌ Invalid selection. Please try again.{Colors.RESET}")
                    
            except (ValueError, KeyboardInterrupt):
                print(f"\n{Colors.RED}❌ Invalid input. Please try again.{Colors.RESET}")
    
    def _display_header(self) -> None:
        """Display application header with system information."""
        system, version = self._detect_os_version()
        
        print(f"\n{Colors.CYAN}{'═' * 70}")
        print(f"  🔍 Automatic Edge Browser Web Searcher (Python)")
        print(f"  🐍 Python {sys.version.split()[0]} | {system} {version}")
        print(f"{'═' * 70}{Colors.RESET}\n")
    
    async def _perform_search(self, page: Page, query: str, search_num: int, total: int) -> bool:
        """Perform a single search with human-like behavior."""
        print(f"{Colors.BLUE}🔍 Search {search_num}/{total}: {query[:50]}{'...' if len(query) > 50 else ''}{Colors.RESET}")
        
        # Navigate to Bing with retry logic
        for attempt in range(3):
            try:
                await page.goto("https://www.bing.com", timeout=30000)
                break
            except Exception as e:
                if attempt == 2:
                    self.logger.error(f"Failed to navigate to Bing after 3 attempts: {e}")
                    return False
                await asyncio.sleep(2)
        
        try:
            # Wait for search box and perform search
            search_box = page.locator("#sb_form_q")
            await search_box.wait_for(state="visible", timeout=30000)
            
            # Clear and type query with human-like delays
            await search_box.fill("")
            await asyncio.sleep(random.uniform(0.1, 0.3))
            
            # Type with random delays between characters
            for char in query:
                await search_box.type(char)
                await asyncio.sleep(random.uniform(0.02, 0.08))
            
            await asyncio.sleep(random.uniform(0.2, 0.5))
            await search_box.press("Enter")
            
            # Wait for page load
            await page.wait_for_load_state("networkidle", timeout=30000)
            await asyncio.sleep(3)  # Additional wait as in original
            
            return True
            
        except Exception as e:
            self.logger.error(f"Error during search '{query}': {e}")
            return False
    
    async def run_automation(self) -> None:
        """Main automation workflow."""
        self._display_header()
        
        # Load queries
        self.queries = self._load_queries()
        if not self.queries:
            print(f"{Colors.RED}❌ No queries available. Exiting.{Colors.RESET}")
            return
        
        print(f"{Colors.GREEN}✓ Loaded {len(self.queries)} unique queries{Colors.RESET}")
        
        # Select profile and get configuration
        selected_profile, use_direct, search_count = self._select_profile()
        if not selected_profile:
            return
        
        self.use_direct_profile = use_direct
        
        # Create temp profile
        try:
            temp_profile = self._copy_profile_safely(selected_profile)
        except Exception as e:
            print(f"{Colors.RED}❌ Failed to create profile copy: {e}{Colors.RESET}")
            return
        
        print(f"\n{Colors.MAGENTA}🚀 Starting {search_count} searches...{Colors.RESET}\n")
        
        # Start Playwright automation
        async with async_playwright() as p:
            try:
                # Launch browser with Edge
                browser = await p.chromium.launch_persistent_context(
                    user_data_dir=str(temp_profile),
                    headless=False,
                    channel="msedge",
                    args=[
                        "--no-sandbox",
                        "--disable-features=ImprovedCookieControls,LazyFrameLoading",
                        "--disable-hang-monitor",
                        "--disable-popup-blocking",
                        "--disable-sync",
                        "--no-first-run",
                        "--password-store=basic"
                    ]
                )
                
                # Get or create page
                page = browser.pages[0] if browser.pages else await browser.new_page()
                
                # Perform searches
                available_queries = self.queries.copy()
                successful_searches = 0
                
                for i in range(search_count):
                    if not available_queries:
                        print(f"{Colors.YELLOW}⚠️  No more unique queries available.{Colors.RESET}")
                        break
                    
                    # Select random query
                    query = random.choice(available_queries)
                    available_queries.remove(query)
                    
                    # Perform search
                    if await self._perform_search(page, query, i + 1, search_count):
                        successful_searches += 1
                    
                    # Random delay between searches
                    if i < search_count - 1:
                        delay = random.uniform(1, 3)
                        await asyncio.sleep(delay)
                
                await browser.close()
                
                print(f"\n{Colors.GREEN}🎉 Completed {successful_searches}/{search_count} searches successfully!{Colors.RESET}")
                
            except Exception as e:
                self.logger.error(f"Browser automation error: {e}")
                print(f"{Colors.RED}❌ Automation failed: {e}{Colors.RESET}")

async def main() -> None:
    """Main entry point for the application."""
    run_as_admin_if_not()  # Ensure script runs with admin privileges on Windows
    searcher = EdgeSearcher()
    await searcher.run_automation()

if __name__ == "__main__":
    asyncio.run(main())