import ctypes
import sys
import os
import psutil # Added psutil

def is_admin():
    """Checks if the current script is running with admin privileges."""
    try:
        return ctypes.windll.shell32.IsUserAnAdmin()
    except:
        return False

def run_as_admin():
    """Re-runs the current script with admin privileges if not already admin."""
    if not is_admin():
        # Re-run the script with admin rights
        ctypes.windll.shell32.ShellExecuteW(None, "runas", sys.executable, " ".join(sys.argv), None, 1)
        sys.exit() # Exit the non-admin instance

def find_and_kill_locking_process(file_name_to_check):
    """Finds and offers to kill processes locking the specified file."""
    if not file_name_to_check:
        print("No file name provided to check.")
        return

    print(f"Searching for processes locking '{file_name_to_check}'...")
    found_processes = []
    for proc in psutil.process_iter(['pid', 'name', 'open_files']):
        try:
            if proc.info['open_files']:
                for f in proc.info['open_files']():
                    if file_name_to_check.lower() in f.path.lower():
                        found_processes.append(proc)
                        break # Found the file in this process, move to next process
        except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
            pass # Ignore processes that have died or we don't have access to

    if not found_processes:
        print(f"No processes found locking a file containing the name '{file_name_to_check}'.")
        return

    print("Found the following process(es) potentially locking the file:")
    for i, proc in enumerate(found_processes):
        print(f"  {i+1}. PID: {proc.info['pid']}, Name: {proc.info['name']}")

    while True:
        try:
            choice = input("Enter the number of the process to kill (or 'c' to cancel): ").strip().lower()
            if choice == 'c':
                print("Operation cancelled.")
                break
            proc_index = int(choice) - 1
            if 0 <= proc_index < len(found_processes):
                target_proc = found_processes[proc_index]
                try:
                    print(f"Attempting to kill process {target_proc.info['name']} (PID: {target_proc.info['pid']})...")
                    target_proc.kill()
                    target_proc.wait(timeout=3) # Wait for termination
                    print(f"Process {target_proc.info['name']} (PID: {target_proc.info['pid']}) terminated successfully.")
                except psutil.NoSuchProcess:
                    print(f"Process {target_proc.info['name']} (PID: {target_proc.info['pid']}) no longer exists.")
                except psutil.AccessDenied:
                    print(f"Access denied. Could not kill process {target_proc.info['name']} (PID: {target_proc.info['pid']}). Ensure you have sufficient privileges.")
                except Exception as e:
                    print(f"Failed to kill process {target_proc.info['name']} (PID: {target_proc.info['pid']}): {e}")
                break # Exit loop after attempting to kill or cancelling
            else:
                print("Invalid selection. Please try again.")
        except ValueError:
            print("Invalid input. Please enter a number or 'c'.")
        except Exception as e:
            print(f"An unexpected error occurred: {e}")
            break

if __name__ == "__main__":
    run_as_admin()
    print("Running with administrator privileges.")
    
    # Get the file name from the user or command-line arguments
    if len(sys.argv) > 1:
        locked_file_name = sys.argv[1]
        print(f"File to check passed as argument: {locked_file_name}")
    else:
        locked_file_name = input("Enter the name of the locked file (e.g., Cookies, History): ").strip()
    
    if locked_file_name:
        find_and_kill_locking_process(locked_file_name)
    else:
        print("No file name entered. Exiting.")

    input("Press Enter to exit...") # Keep window open