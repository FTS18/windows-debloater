# ApexDebloater - Windows 10 and 11 Optimization Utility

ApexDebloater is a lightweight, modern, and high-performance WPF utility designed to optimize, customize, and debloat Windows 10 and 11 environments. It allows you to reclaim system memory, reduce background CPU usage, remove pre-installed bloatware apps, block tracking telemetry, and manage startup programs.

---

## Features

### Privacy and Telemetry Blocker
* Disables Windows diagnostic data collections, advertising ID tracking, and telemetry services.
* Redirects known Microsoft telemetry, advertising, and tracking domains to 0.0.0.0 at the hosts file level.
* Disables Bing Search integration within the Windows Start Menu to prevent local search queries from being sent online.
* Disables Lock Screen promotional ads, suggestions, and Windows Spotlight consumer features.

### Bloatware (AppX) Uninstaller
* Scans for pre-installed UWP/AppX packages and system bloatware.
* Allows safe, custom selection for batch uninstallation or complete package restoration.
* Keeps track of active console logs to monitor Winget installation and uninstallation tasks.

### Disk and Component Store Cleaner
* Scans and deletes temporary directories, system log files, prefetch cache, and error reporting data.
* Integrates Windows Recycle Bin cleanup.
* Includes Windows Component Store (WinSxS) Cleanup via DISM to compress and clean up obsolete Windows Update backups.

### Startup Apps Manager
* Scans Windows startup registry keys (Run, RunOnce) and startup directory shortcuts.
* Allows enabling or disabling background apps to improve boot times and free up system memory.

### Context Menu Cleaner (Shell Extensions)
* Resolves raw registry CLSID GUIDs to friendly, readable names using dll descriptions.
* Allows toggling third-party context menu shell extensions to clean up your right-click Explorer menu.

### System Utilities and Repair
* Runs System File Checker (SFC) scans to fix corrupted system files.
* Runs Deployment Image Servicing and Management (DISM) to check and repair system component store health.
* Unlocks the hidden Windows Ultimate Performance power scheme to maximize hardware throughput.
* Creates System Restore Points automatically or manually as a safety checkpoint before applying optimizations.

### DNS Switcher
* Quickly changes DNS configurations to secure, privacy-respecting servers (Cloudflare, Google, Quad9) or resets back to default ISP DHCP settings across all connected network adapters.

### Live Hardware Monitor
* Displays real-time CPU load, physical memory (RAM) usage, and system disk storage metrics on the dashboard using safe Win32 kernel calls.

---

## Getting Started

### Prerequisites
* Windows 10 or Windows 11 (64-bit)
* .NET 10.0 Desktop Runtime or SDK (to build from source)
* Local Administrator privileges (required to toggle system services and modify policy registry keys)

### Installation and Building
1. Clone this repository:
   ```bash
   git clone https://github.com/yourusername/ApexDebloater.git
   cd ApexDebloater
   ```

2. Build the project in Release configuration:
   ```bash
   dotnet build -c Release
   ```

3. Run the compiled executable as Administrator:
   * The output binary is located at `bin\Release\net10.0-windows\ApexDebloater.exe`.
   * Right-click the file and select "Run as Administrator" to ensure all registry and service modifications can execute.

---

## Safety and Backups

Before applying any system optimizations:
1. ApexDebloater offers a one-click button to create a System Restore Point.
2. The application backs up all altered registry states. You can use the "Restore Backups" option under the Tools tab to revert configurations back to default settings at any time.

---

## License

This project is licensed under the MIT License. See the LICENSE file for details.
