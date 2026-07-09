using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ApexDebloater
{
    public partial class MainWindow : Window
    {
        private readonly DebloatEngine _engine;
        private readonly List<SoftwareItem> _defaultSoftware = new()
        {
            new SoftwareItem { Id = "Microsoft.VisualStudioCode", Name = "VS Code" },
            new SoftwareItem { Id = "Git.Git", Name = "Git SCM" },
            new SoftwareItem { Id = "Google.Chrome", Name = "Chrome" },
            new SoftwareItem { Id = "Mozilla.Firefox", Name = "Firefox" },
            new SoftwareItem { Id = "VideoLAN.VLC", Name = "VLC Player" },
            new SoftwareItem { Id = "7zip.7zip", Name = "7-Zip" },
            new SoftwareItem { Id = "Discord.Discord", Name = "Discord" },
            new SoftwareItem { Id = "Valve.Steam", Name = "Steam" },
            new SoftwareItem { Id = "OpenJS.NodeJS.LTS", Name = "Node.js" }
        };
        private System.Threading.CancellationTokenSource? _searchCts;

        public MainWindow()
        {
            InitializeComponent();
            _engine = new DebloatEngine();
            TweaksItemsControl.ItemsSource = _engine.Tweaks;
            SoftwareItemsControl.ItemsSource = _defaultSoftware;
            PopulateSystemInfo();
            CheckWingetPresence();
            StartHardwareMonitor();
            SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            UpdateTitleBarTheme(ThemeToggle.IsChecked ?? true);
        }

        private readonly List<(string Name, string Id)> _installedPackages = new();

        private string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            return name.Replace("Scm", "", StringComparison.OrdinalIgnoreCase)
                       .Replace("Player", "", StringComparison.OrdinalIgnoreCase)
                       .Replace("Browser", "", StringComparison.OrdinalIgnoreCase)
                       .Replace(" ", "")
                       .Trim();
        }

        private bool IsAppMatch(string appId, string appName, string installedId, string installedName)
        {
            // 1. Exact ID match
            if (string.Equals(appId, installedId, StringComparison.OrdinalIgnoreCase))
                return true;

            // 2. Contains ID match (e.g. Google.Chrome.EXE containing Google.Chrome)
            if (installedId.Contains(appId, StringComparison.OrdinalIgnoreCase) || 
                appId.Contains(installedId, StringComparison.OrdinalIgnoreCase))
                return true;

            // 3. Name matching
            string cleanAppName = CleanName(appName);
            string cleanInstalledName = CleanName(installedName);

            if (string.Equals(cleanAppName, cleanInstalledName, StringComparison.OrdinalIgnoreCase))
                return true;

            // 4. Special cases (like VS Code vs Visual Studio Code)
            if (appName.Equals("VS Code", StringComparison.OrdinalIgnoreCase) || 
                appName.Contains("Visual Studio Code", StringComparison.OrdinalIgnoreCase))
            {
                if (installedName.Contains("Visual Studio Code", StringComparison.OrdinalIgnoreCase) || 
                    installedId.Contains("VisualStudioCode", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            if (appName.Equals("Git SCM", StringComparison.OrdinalIgnoreCase) || 
                appName.Equals("Git", StringComparison.OrdinalIgnoreCase))
            {
                if (installedName.Equals("Git", StringComparison.OrdinalIgnoreCase) || 
                    installedId.Contains("Git.Git", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool CheckIsInstalled(string appId, string appName)
        {
            lock (_installedPackages)
            {
                foreach (var pkg in _installedPackages)
                {
                    if (IsAppMatch(appId, appName, pkg.Id, pkg.Name))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task LoadInstalledPackagesAsync()
        {
            try
            {
                string output = await Task.Run(() => DebloatEngine.RunPowerShellCommand("winget list"));
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool startParsing = false;
                var list = new List<(string Name, string Id)>();

                foreach (var line in lines)
                {
                    if (line.Contains("---"))
                    {
                        startParsing = true;
                        continue;
                    }
                    if (!startParsing) continue;

                    var parts = System.Text.RegularExpressions.Regex.Split(line, @"\s{2,}");
                    if (parts.Length >= 2)
                    {
                        string name = parts[0].Trim();
                        string id = parts[1].Trim();
                        if (!string.IsNullOrEmpty(id) && !id.StartsWith("ARP\\"))
                        {
                            list.Add((name, id));
                        }
                    }
                }

                lock (_installedPackages)
                {
                    _installedPackages.Clear();
                    _installedPackages.AddRange(list);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load installed packages: {ex.Message}");
            }
        }

        private async void PopulateSystemInfo()
        {
            ProfileName.Text = Environment.UserName;
            DeviceName.Text = Environment.MachineName;
            DeviceModel.Text = "Loading System Specs...";

            try
            {
                string fullName = await Task.Run(() => DebloatEngine.GetUserFullName());
                string manufacturer = await Task.Run(() => DebloatEngine.GetSystemManufacturer());
                string model = await Task.Run(() => DebloatEngine.GetSystemModel());

                await Task.Run(() => _engine.CheckAllAppliedStates());
                await LoadInstalledPackagesAsync();

                foreach (var app in _defaultSoftware)
                {
                    if (CheckIsInstalled(app.Id, app.Name))
                    {
                        app.IsInstalled = true;
                        app.IsChecked = true;
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    ProfileName.Text = fullName;
                    DeviceModel.Text = $"{manufacturer} {model}".Trim();
                    RecalculateHealthScore();
                    
                    // Refresh default list with installed checks
                    SoftwareItemsControl.ItemsSource = null;
                    SoftwareItemsControl.ItemsSource = _defaultSoftware;
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    DeviceModel.Text = "Windows PC";
                });
            }
        }

        // --- WINDOW WINDOWCONTROLS & DRAG ---

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        // --- DYNAMIC THEME SWITCHING ---

        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            SetTheme(true);
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            SetTheme(false);
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] MEMORYSTATUSEX lpBuffer);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
            public ulong Value => ((ulong)dwHighDateTime << 32) | dwLowDateTime;
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

        private System.Windows.Threading.DispatcherTimer? _hardwareTimer;
        private FILETIME _prevIdleTime;
        private FILETIME _prevKernelTime;
        private FILETIME _prevUserTime;
        private bool _hasPrevTimes = false;

        private void StartHardwareMonitor()
        {
            _hardwareTimer = new System.Windows.Threading.DispatcherTimer();
            _hardwareTimer.Interval = TimeSpan.FromSeconds(2);
            _hardwareTimer.Tick += HardwareTimer_Tick;
            _hardwareTimer.Start();
            UpdateHardwareStats();
        }

        private void HardwareTimer_Tick(object? sender, EventArgs e)
        {
            UpdateHardwareStats();
        }

        private void UpdateHardwareStats()
        {
            double cpuVal = GetCpuUsageInternal();
            CpuText.Text = $"{cpuVal:F0}%";
            CpuBar.Value = cpuVal;

            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                double totalGb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                double availGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                double usedGb = totalGb - availGb;
                
                RamText.Text = $"{usedGb:F1} GB / {totalGb:F1} GB";
                RamBar.Value = memStatus.dwMemoryLoad;
                RamSubtext.Text = $"{memStatus.dwMemoryLoad}% utilized";
            }

            try
            {
                var systemDrive = new System.IO.DriveInfo("C");
                double freeGb = systemDrive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                double totalGb = systemDrive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                double usedGb = totalGb - freeGb;
                double usedPercent = (usedGb / totalGb) * 100.0;

                StorageText.Text = $"{freeGb:F0} GB Free";
                StorageBar.Value = usedPercent;
                StorageSubtext.Text = $"{usedGb:F0} GB of {totalGb:F0} GB used";
            }
            catch
            {
                StorageText.Text = "N/A";
                StorageBar.Value = 0;
                StorageSubtext.Text = "System Drive C: not found";
            }
        }

        private double GetCpuUsageInternal()
        {
            if (!GetSystemTimes(out var idle, out var kernel, out var user))
                return 0;

            if (!_hasPrevTimes)
            {
                _prevIdleTime = idle;
                _prevKernelTime = kernel;
                _prevUserTime = user;
                _hasPrevTimes = true;
                return 0;
            }

            ulong idleDiff = idle.Value - _prevIdleTime.Value;
            ulong kernelDiff = kernel.Value - _prevKernelTime.Value;
            ulong userDiff = user.Value - _prevUserTime.Value;

            _prevIdleTime = idle;
            _prevKernelTime = kernel;
            _prevUserTime = user;

            ulong totalSys = kernelDiff + userDiff;
            if (totalSys == 0) return 0;

            double cpu = 1.0 - ((double)idleDiff / totalSys);
            return Math.Clamp(cpu * 100.0, 0.0, 100.0);
        }

        private void UpdateTitleBarTheme(bool isDark)
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                IntPtr hwnd = helper.Handle;
                if (hwnd == IntPtr.Zero) return;

                int darkValue = isDark ? 1 : 0;
                
                int result = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkValue, sizeof(int));
                if (result != 0)
                {
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkValue, sizeof(int));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set title bar theme: {ex.Message}");
            }
        }

        private void SetTheme(bool isDark)
        {
            try
            {
                var dict = new ResourceDictionary();
                if (isDark)
                {
                    dict.Source = new Uri("Styles/DarkTheme.xaml", UriKind.Relative);
                }
                else
                {
                    dict.Source = new Uri("Styles/LightTheme.xaml", UriKind.Relative);
                }

                // Replace the theme dictionary (which is the first one in App.xaml)
                Application.Current.Resources.MergedDictionaries[0] = dict;
                
                // Update native Windows OS title bar theme to match
                UpdateTitleBarTheme(isDark);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching theme: {ex.Message}", "Theme Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void FlushRam_Click(object sender, RoutedEventArgs e)
        {
            DashboardStatusText.Text = "Flushing Standby Memory Cache...";
            try
            {
                long bytesFreed = await DebloatEngine.FlushMemoryAsync((log) =>
                {
                    Dispatcher.Invoke(() => DashboardStatusText.Text = log.Trim());
                });
                
                DashboardStatusText.Text = $"Memory Cache Flushed! Freed {(bytesFreed / (1024.0 * 1024.0)):F1} MB of RAM.";
            }
            catch (Exception ex)
            {
                DashboardStatusText.Text = $"Error flushing memory: {ex.Message}";
            }
        }

        private async void CreateRestore_Click(object sender, RoutedEventArgs e)
        {
            DashboardStatusText.Text = "Creating System Restore Point... (This may take a minute)";
            try
            {
                await DebloatEngine.CreateRestorePointAsync();
                DashboardStatusText.Text = "System Restore Point created successfully.";
            }
            catch (Exception ex)
            {
                DashboardStatusText.Text = $"Error: {ex.Message}";
            }
        }

        private async void QuickOptimize_Click(object sender, RoutedEventArgs e)
        {
            DashboardStatusText.Text = "Applying safe recommended optimizations...";
            try
            {
                await Task.Run(() =>
                {
                    // Create Restore Point First for safety
                    Dispatcher.Invoke(() => DashboardStatusText.Text = "Creating backup Restore Point...");
                    DebloatEngine.CreateRestorePointAsync().Wait();

                    // Apply all Safe tweaks
                    var safeTweaks = _engine.Tweaks.Where(t => t.Risk == "Safe").ToList();
                    foreach (var tweak in safeTweaks)
                    {
                        Dispatcher.Invoke(() => DashboardStatusText.Text = $"Applying: {tweak.Name}...");
                        tweak.ApplyAction?.Invoke();
                        tweak.IsApplied = true;
                    }
                });

                DashboardStatusText.Text = "Recommended optimization applied successfully! Explorer restarted.";
                RecalculateHealthScore();
            }
            catch (Exception ex)
            {
                DashboardStatusText.Text = $"Failed: {ex.Message}";
            }
        }

        // --- TAB 2: SYSTEM TWEAKS ---

        private async void ApplyTweaks_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to apply the selected configuration tweaks? A restore point is recommended before executing.",
                "Confirm Operation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return;

            var tweaks = _engine.Tweaks.ToList();
            await Task.Run(() =>
            {
                foreach (var tweak in tweaks)
                {
                    try
                    {
                        if (tweak.IsApplied)
                        {
                            tweak.ApplyAction?.Invoke();
                        }
                        else
                        {
                            tweak.UndoAction?.Invoke();
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show($"Error applying tweak '{tweak.Name}': {ex.Message}"));
                    }
                }
                
                DebloatEngine.RestartExplorer();
            });

            MessageBox.Show("Tweaks configuration updated successfully!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            RecalculateHealthScore();
        }

        // --- TAB 3: DISK CLEANER ---

        private async void RunCleaner_Click(object sender, RoutedEventArgs e)
        {
            CleanerLogs.Text = "";
            
            // Collect targets to clean
            bool cleanUser = CleanUserTemp.IsChecked ?? false;
            bool cleanSys = CleanSystemTemp.IsChecked ?? false;
            bool cleanPref = CleanPrefetch.IsChecked ?? false;
            bool cleanUpd = CleanUpdates.IsChecked ?? false;
            bool cleanRecycle = CleanRecycleBin.IsChecked ?? false;
            bool cleanComponent = CleanComponentStore.IsChecked ?? false;
            bool cleanDns = CleanDns.IsChecked ?? false;

            await Task.Run(() =>
            {
                long totalFreed = 0;

                string userTemp = System.IO.Path.GetTempPath();
                string systemTemp = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                string prefetch = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                string updateDownload = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");

                LogCleaner("Starting Disk Cleanup...\n");

                if (cleanUser) totalFreed += CleanDirectory(userTemp);
                if (cleanSys) totalFreed += CleanDirectory(systemTemp);
                if (cleanPref) totalFreed += CleanDirectory(prefetch);
                if (cleanUpd) totalFreed += CleanDirectory(updateDownload);

                if (cleanRecycle)
                {
                    LogCleaner("\nEmptying Recycle Bin...\n");
                    try
                    {
                        int result = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOSOUND);
                        if (result == 0)
                        {
                            LogCleaner("Recycle Bin successfully emptied.\n");
                        }
                        else
                        {
                            LogCleaner("Recycle Bin is already empty or could not be emptied.\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogCleaner($"Failed to empty Recycle Bin: {ex.Message}\n");
                    }
                }

                if (cleanComponent)
                {
                    LogCleaner("\nCleaning Windows Component Store (DISM cleanup; this may take 1-2 minutes)...\n");
                    RunProcess("dism.exe", "/online /Cleanup-Image /StartComponentCleanup /ResetBase");
                    LogCleaner("Component Store Cleanup finished.\n");
                }

                if (cleanDns)
                {
                    LogCleaner("\nFlushing DNS Cache...\n");
                    RunProcess("ipconfig.exe", "/flushdns");
                    LogCleaner("DNS Cache successfully flushed.\n");
                }

                LogCleaner("\nCleaning Windows Error Reporting logs...\n");
                string werPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "WER");
                totalFreed += CleanDirectory(werPath);

                LogCleaner($"\nCleanup Completed! Freed {(totalFreed / (1024.0 * 1024.0)):F2} MB.\n");
            });
        }

        private void LogCleaner(string text)
        {
            Dispatcher.Invoke(() =>
            {
                CleanerLogs.Text += text;
                CleanerScroller.ScrollToEnd();
            });
        }

        private long CleanDirectory(string path)
        {
            long freed = 0;
            if (!System.IO.Directory.Exists(path)) return 0;

            LogCleaner($"Scanning & Cleaning: {path}...\n");

            // Clean Files
            try
            {
                var files = System.IO.Directory.GetFiles(path);
                foreach (var file in files)
                {
                    try
                    {
                        var info = new System.IO.FileInfo(file);
                        long size = info.Length;
                        System.IO.File.Delete(file);
                        freed += size;
                        LogCleaner($"Deleted File: {System.IO.Path.GetFileName(file)}\n");
                    }
                    catch
                    {
                        // File locked or inaccessible
                    }
                }
            }
            catch (Exception ex)
            {
                LogCleaner($"Failed to clean files in {path}: {ex.Message}\n");
            }

            // Clean Subdirectories
            try
            {
                var subDirs = System.IO.Directory.GetDirectories(path);
                foreach (var dir in subDirs)
                {
                    try
                    {
                        long size = GetDirectorySize(dir);
                        System.IO.Directory.Delete(dir, true);
                        freed += size;
                        LogCleaner($"Deleted Directory: {System.IO.Path.GetFileName(dir)}\n");
                    }
                    catch
                    {
                        // Directory locked
                    }
                }
            }
            catch (Exception ex)
            {
                LogCleaner($"Failed to clean folders in {path}: {ex.Message}\n");
            }

            return freed;
        }

        private long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                var files = System.IO.Directory.GetFiles(path, "*", System.IO.SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        size += new System.IO.FileInfo(file).Length;
                    }
                    catch { }
                }
            }
            catch { }
            return size;
        }

        private void RunProcess(string filename, string arguments)
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                process?.WaitForExit();
            }
            catch { }
        }

        // --- TAB 4: APPX MANAGER ---

        private void AppType_Changed(object sender, RoutedEventArgs e)
        {
            if (AppxListBox == null) return;
            AppxListBox.ItemsSource = null;
            AppxPlaceholderText.Visibility = Visibility.Visible;
            
            if (RadioStoreApps.IsChecked == true)
            {
                AppManagerTitle.Text = "AppX Packages";
                AppManagerSubtitle.Text = "Manage built-in Windows UWP Store bloatware. Scan and uninstall selected items.";
                ScanAppsButton.Content = "Scan for Bloatware";
                ReinstallAppsButton.Visibility = Visibility.Visible;
                AppxPlaceholderText.Text = "Click 'Scan for Bloatware' below to populate list.";
                AppxLogs.Text = "AppX Console Ready.";
            }
            else
            {
                AppManagerTitle.Text = "Desktop Applications";
                AppManagerSubtitle.Text = "Manage standard desktop software installed on your system. Perform silent uninstalls.";
                ScanAppsButton.Content = "Scan for Desktop Apps";
                ReinstallAppsButton.Visibility = Visibility.Collapsed;
                AppxPlaceholderText.Text = "Click 'Scan for Desktop Apps' below to populate list.";
                AppxLogs.Text = "Desktop Uninstaller Console Ready.";
            }
        }

        private async void ScanAppx_Click(object sender, RoutedEventArgs e)
        {
            AppxPlaceholderText.Text = "Scanning system packages. Please wait...";
            AppxPlaceholderText.Visibility = Visibility.Visible;
            AppxListBox.ItemsSource = null;

            try
            {
                if (RadioStoreApps.IsChecked == true)
                {
                    var apps = await DebloatEngine.GetAppxPackagesAsync();
                    if (apps.Count > 0)
                    {
                        AppxListBox.ItemsSource = apps;
                        AppxPlaceholderText.Visibility = Visibility.Collapsed;
                        AppxLogs.Text = $"Found {apps.Count} UWP packages. Recommended apps to remove are pre-checked.";
                    }
                    else
                    {
                        AppxPlaceholderText.Text = "No UWP packages found on your system.";
                        AppxLogs.Text = "Scan finished: No matching UWP packages found.";
                    }
                }
                else
                {
                    var apps = await DebloatEngine.GetDesktopApplicationsAsync();
                    if (apps.Count > 0)
                    {
                        AppxListBox.ItemsSource = apps;
                        AppxPlaceholderText.Visibility = Visibility.Collapsed;
                        AppxLogs.Text = $"Found {apps.Count} desktop applications.";
                    }
                    else
                    {
                        AppxPlaceholderText.Text = "No desktop applications found.";
                        AppxLogs.Text = "Scan finished: No desktop applications found.";
                    }
                }
            }
            catch (Exception ex)
            {
                AppxPlaceholderText.Text = "Failed to scan packages.";
                AppxLogs.Text = $"Error: {ex.Message}";
            }
        }

        private async void RemoveAppx_Click(object sender, RoutedEventArgs e)
        {
            var selectedApps = (AppxListBox.ItemsSource as IEnumerable<AppxPackageItem>)?
                .Where(x => x.IsChecked)
                .ToList();

            if (selectedApps == null || selectedApps.Count == 0)
            {
                MessageBox.Show("No apps selected for removal.", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AppxLogs.Text = $"Starting removal of {selectedApps.Count} applications...\n";

            if (RadioStoreApps.IsChecked == true)
            {
                foreach (var app in selectedApps)
                {
                    await DebloatEngine.RemoveAppxPackageAsync(app.Name, (log) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AppxLogs.Text += log;
                            AppxScroller.ScrollToEnd();
                        });
                    });
                }
            }
            else
            {
                foreach (var app in selectedApps)
                {
                    await DebloatEngine.RemoveDesktopAppAsync(app.DisplayName, app.UninstallString ?? "", (log) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AppxLogs.Text += log;
                            AppxScroller.ScrollToEnd();
                        });
                    });
                }
            }

            AppxLogs.Text += "\nUninstall process complete! Rescanning...\n";
            ScanAppx_Click(sender, e);
        }

        // --- TAB 5: SOFTWARE INSTALLER ---

        private async void SoftwareSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchCts?.Cancel();
            _searchCts = new System.Threading.CancellationTokenSource();
            var token = _searchCts.Token;

            string query = SoftwareSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                foreach (var app in _defaultSoftware)
                {
                    app.IsInstalled = CheckIsInstalled(app.Id, app.Name);
                }
                SoftwareItemsControl.ItemsSource = null;
                SoftwareItemsControl.ItemsSource = _defaultSoftware;
                return;
            }

            try
            {
                // Debounce search by 400ms
                await Task.Delay(400, token);
                if (token.IsCancellationRequested) return;

                Dispatcher.Invoke(() =>
                {
                    SoftwareSearchLoadingOverlay.Visibility = Visibility.Visible;
                    SoftwareItemsControl.ItemsSource = null;
                });

                var results = await DebloatEngine.SearchWingetAsync(query);
                if (token.IsCancellationRequested) return;

                foreach (var app in results)
                {
                    if (CheckIsInstalled(app.Id, app.Name))
                    {
                        app.IsInstalled = true;
                        app.IsChecked = true;
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    SoftwareSearchLoadingOverlay.Visibility = Visibility.Collapsed;
                    SoftwareItemsControl.ItemsSource = results;
                });
            }
            catch (TaskCanceledException) {}
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    SoftwareSearchLoadingOverlay.Visibility = Visibility.Collapsed;
                });
            }
        }

        private async void InstallSoftware_Click(object sender, RoutedEventArgs e)
        {
            var selected = (SoftwareItemsControl.ItemsSource as IEnumerable<SoftwareItem>)?
                .Where(x => x.IsChecked && !x.IsInstalled)
                .ToList();

            if (selected == null || selected.Count == 0)
            {
                MessageBox.Show("Please select at least one software package to install.", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            WingetLogs.Text = $"Starting installation of {selected.Count} packages...\n";
            InstallButton.IsEnabled = false;

            foreach (var app in selected)
            {
                await DebloatEngine.InstallSoftwareAsync(app.Id, (log) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        WingetLogs.Text += log;
                        WingetScroller.ScrollToEnd();
                    });
                });

                lock (_installedPackages)
                {
                    _installedPackages.Add((app.Name, app.Id));
                    app.IsInstalled = true;
                    app.IsChecked = true;
                }
            }

            InstallButton.IsEnabled = true;
            WingetLogs.Text += "\nInstallation batch complete!\n";

            // Refresh view to show updated installed badges
            var current = SoftwareItemsControl.ItemsSource;
            SoftwareItemsControl.ItemsSource = null;
            SoftwareItemsControl.ItemsSource = current;
        }

        // --- TAB 6: SYSTEM REPAIR ---

        private async void RunSfc_Click(object sender, RoutedEventArgs e)
        {
            RepairLogs.Text = "";
            await DebloatEngine.RunSystemRepairAsync("SFC", (log) =>
            {
                Dispatcher.Invoke(() =>
                {
                    RepairLogs.Text += log;
                    RepairScroller.ScrollToEnd();
                });
            });
        }

        private async void RunDism_Click(object sender, RoutedEventArgs e)
        {
            RepairLogs.Text = "";
            await DebloatEngine.RunSystemRepairAsync("DISM", (log) =>
            {
                Dispatcher.Invoke(() =>
                {
                    RepairLogs.Text += log;
                });
            });
        }

        private async void CreateRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            RepairLogs.Text = "Requesting System Restore Point creation...\n";
            try
            {
                await Task.Run(() =>
                {
                    DebloatEngine.CreateRestorePointAsync().Wait();
                });
                RepairLogs.Text += "System Restore Point successfully created.\n";
            }
            catch (Exception ex)
            {
                RepairLogs.Text += $"Failed to create Restore Point: {ex.Message}\n";
            }
        }

        private async void UnlockUltimatePerformance_Click(object sender, RoutedEventArgs e)
        {
            RepairLogs.Text = "Unlocking Ultimate Performance Power Plan...\n";
            try
            {
                await Task.Run(() =>
                {
                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = System.Diagnostics.Process.Start(processInfo);
                    string output = proc?.StandardOutput.ReadToEnd() ?? "";
                    proc?.WaitForExit();

                    string guid = "e9a42b02-d5df-448d-aa00-03f14749eb61";
                    if (output.Contains("GUID:"))
                    {
                        var parts = output.Split(new[] { "GUID:" }, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            guid = parts[1].Trim().Split(' ')[0];
                        }
                    }
                    
                    var activeInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"/setactive {guid}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var activeProc = System.Diagnostics.Process.Start(activeInfo);
                    activeProc?.WaitForExit();
                });
                RepairLogs.Text += "Ultimate Performance Power Plan successfully activated.\n";
            }
            catch (Exception ex)
            {
                RepairLogs.Text += $"Failed to activate power scheme: {ex.Message}\n";
            }
        }

        private async void ApplyDns_Click(object sender, RoutedEventArgs e)
        {
            if (DnsComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string choice = selectedItem.Content.ToString()!;
                RepairLogs.Text = $"Applying DNS profile: {choice}...\n";
                
                await Task.Run(() =>
                {
                    string psCommand = "";
                    if (choice.Contains("Default"))
                    {
                        psCommand = "Get-NetIPInterface -ConnectionState Connected -AddressFamily IPv4 | Foreach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ResetServerAddresses -ErrorAction SilentlyContinue }";
                    }
                    else if (choice.Contains("Cloudflare"))
                    {
                        psCommand = "Get-NetIPInterface -ConnectionState Connected -AddressFamily IPv4 | Foreach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses ('1.1.1.1', '1.0.0.1') -ErrorAction SilentlyContinue }";
                    }
                    else if (choice.Contains("Google"))
                    {
                        psCommand = "Get-NetIPInterface -ConnectionState Connected -AddressFamily IPv4 | Foreach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses ('8.8.8.8', '8.8.4.4') -ErrorAction SilentlyContinue }";
                    }
                    else if (choice.Contains("Quad9"))
                    {
                        psCommand = "Get-NetIPInterface -ConnectionState Connected -AddressFamily IPv4 | Foreach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses ('9.9.9.9', '149.112.112.112') -ErrorAction SilentlyContinue }";
                    }

                    if (!string.IsNullOrEmpty(psCommand))
                    {
                        var processInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -Command \"{psCommand}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var proc = System.Diagnostics.Process.Start(processInfo);
                        proc?.WaitForExit();
                    }
                });
                
                RepairLogs.Text += "DNS settings successfully updated. DNS cache flushed.\n";
                RunProcess("ipconfig.exe", "/flushdns");
            }
        }

        private async void ReinstallAppx_Click(object sender, RoutedEventArgs e)
        {
            var selectedApps = (AppxListBox.ItemsSource as IEnumerable<AppxPackageItem>)?
                .Where(x => x.IsChecked)
                .ToList();

            if (selectedApps == null || selectedApps.Count == 0)
            {
                MessageBox.Show("No packages selected for reinstallation.", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AppxLogs.Text = $"Starting reinstallation of {selectedApps.Count} packages...\n";
            foreach (var app in selectedApps)
            {
                AppxLogs.Text += $"Restoring {app.DisplayName}...\n";
                await Task.Run(() =>
                {
                    DebloatEngine.RunPowerShellCommand($"Get-AppxPackage -AllUsers -Name '*{app.Name}*' | Foreach {{Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -ErrorAction SilentlyContinue}}");
                });
                AppxLogs.Text += $"Finished restore attempt for {app.DisplayName}.\n";
            }

            AppxLogs.Text += "\nReinstall process complete! Rescanning packages...\n";
            ScanAppx_Click(sender, e);
        }

        private async void RestoreBackups_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to restore all previous registry backups? This will override your current settings and restart Windows Explorer.",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return;

            await DebloatEngine.RestoreRegistryBackupsAsync((log) => {});

            MessageBox.Show("Registry backups successfully restored!", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void CheckWingetPresence()
        {
            bool hasWinget = await Task.Run(() =>
            {
                try
                {
                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "where.exe",
                        Arguments = "winget",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var process = System.Diagnostics.Process.Start(processInfo);
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            });

            Dispatcher.Invoke(() =>
            {
                if (!hasWinget)
                {
                    WingetWarningBanner.Visibility = Visibility.Visible;
                    SoftwareSearchBox.IsEnabled = false;
                }
            });
        }



        private async void ScanShellExtensions_Click(object sender, RoutedEventArgs e)
        {
            RepairLogs.Text += "\nScanning third-party context menu shell extensions...\n";
            try
            {
                var items = await DebloatEngine.GetShellExtensionsAsync();
                ShellExtensionsListBox.ItemsSource = items;
                RepairLogs.Text += $"Found {items.Count} context menu extensions.\n";
            }
            catch (Exception ex)
            {
                RepairLogs.Text += $"Shell extension scan failed: {ex.Message}\n";
            }
        }

        private void ToggleShellExtension_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton button && button.DataContext is ShellExtensionItem item)
            {
                bool targetState = button.IsChecked ?? false;
                DebloatEngine.ToggleShellExtension(item, targetState);
                RepairLogs.Text += $"{(targetState ? "Enabled" : "Disabled")} shell extension: {item.Name}\n";
            }
        }

        private void RecalculateHealthScore()
        {
            try
            {
                if (_engine == null || _engine.Tweaks == null || _engine.Tweaks.Count == 0) return;

                int totalTweaks = _engine.Tweaks.Count;
                int appliedTweaks = _engine.Tweaks.Count(t => t.IsApplied);

                double score = 40.0 + ((double)appliedTweaks / totalTweaks * 60.0);
                
                HealthScoreBar.Value = score;
                HealthScoreText.Text = $"{Math.Round(score)}%";
            }
            catch {}
        }

        // --- UTILITY VISUAL HELPERS ---

        private async void ScanStandaloneStartup_Click(object sender, RoutedEventArgs e)
        {
            StandaloneStartupPlaceholderText.Text = "Scanning startup programs. Please wait...";
            StandaloneStartupPlaceholderText.Visibility = Visibility.Visible;
            StandaloneStartupListBox.ItemsSource = null;

            try
            {
                var items = await DebloatEngine.GetStartupItemsAsync();
                if (items.Count > 0)
                {
                    StandaloneStartupListBox.ItemsSource = items;
                    StandaloneStartupPlaceholderText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    StandaloneStartupPlaceholderText.Text = "No startup apps found.";
                }
            }
            catch (Exception ex)
            {
                StandaloneStartupPlaceholderText.Text = "Failed to scan startup apps.";
                System.Diagnostics.Debug.WriteLine($"Startup scan error: {ex.Message}");
            }
        }

        private async void StandaloneStartupToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle && toggle.DataContext is StartupItem item)
            {
                bool isChecked = toggle.IsChecked ?? false;
                item.IsEnabled = isChecked;
                await Task.Run(() => DebloatEngine.ToggleStartupItem(item, isChecked));
            }
        }

        private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t)
                {
                    return t;
                }
                T? childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
    }
}