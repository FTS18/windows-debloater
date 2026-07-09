using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ApexDebloater
{
    public class DebloatEngine
    {
        public List<TweakItem> Tweaks { get; } = new();

        public DebloatEngine()
        {
            InitializeTweaks();
        }

        private void InitializeTweaks()
        {
            // --- PRIVACY & TELEMETRY ---
            Tweaks.Add(new TweakItem
            {
                Id = "Telemetry",
                Name = "Disable Telemetry & Diagnostic Data",
                Description = "Disables Windows feedback, diagnostic data collection, and advertising ID tracking.",
                Category = "Privacy",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord);
                    RunCommand("sc.exe", "config DiagTrack start= disabled");
                    RunCommand("sc.exe", "stop DiagTrack");
                    RunCommand("sc.exe", "config dmwappushservice start= disabled");
                    RunCommand("sc.exe", "stop dmwappushservice");
                },
                UndoAction = () =>
                {
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry");
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 1, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1, RegistryValueKind.DWord);
                    RunCommand("sc.exe", "config DiagTrack start= auto");
                    RunCommand("sc.exe", "start DiagTrack");
                    RunCommand("sc.exe", "config dmwappushservice start= demand");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry") is int val && val == 0
            });

            Tweaks.Add(new TweakItem
            {
                Id = "BingSearch",
                Name = "Disable Bing Start Menu Search",
                Description = "Prevents Windows Search from sending local query terms online to Bing.",
                Category = "Privacy",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0, RegistryValueKind.DWord);
                },
                UndoAction = () =>
                {
                    DeleteRegistryValue(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions");
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 1, RegistryValueKind.DWord);
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions") is int val && val == 1
            });


            // --- WINDOWS 11 CUSTOMIZATIONS ---
            Tweaks.Add(new TweakItem
            {
                Id = "LockScreenAds",
                Name = "Disable Lock Screen Ads & Tips",
                Description = "Stops Windows from displaying 'fun facts', suggestions, and ads on your lock screen.",
                Category = "Customization",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled", 0, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "RotatingLockScreenEnabled", 0, RegistryValueKind.DWord);
                },
                UndoAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled", 1, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 1, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "RotatingLockScreenEnabled", 1, RegistryValueKind.DWord);
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled") is int val && val == 0
            });

            Tweaks.Add(new TweakItem
            {
                Id = "TaskbarSearch",
                Name = "Hide Taskbar Search Box",
                Description = "Hides the search box on the Windows 11 taskbar to save screen space.",
                Category = "Customization",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0, RegistryValueKind.DWord);
                },
                UndoAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1, RegistryValueKind.DWord);
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode") is int val && val == 0
            });
            Tweaks.Add(new TweakItem
            {
                Id = "ClassicMenu",
                Name = "Classic Right-Click Context Menu",
                Description = "Restores the Windows 10 style right-click context menu (no 'Show more options' click).",
                Category = "Customization",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "", "", RegistryValueKind.String);
                },
                UndoAction = () =>
                {
                    DeleteRegistryKey(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}");
                },
                CheckAppliedAction = () => RegistryKeyExists(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}")
            });

            Tweaks.Add(new TweakItem
            {
                Id = "WidgetsChat",
                Name = "Hide Taskbar Widgets & Chat",
                Description = "Hides the MSN Widgets panel and Teams Chat icons from your taskbar.",
                Category = "Customization",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    RunCommand("powershell.exe", "-NoProfile -Command \"Get-AppxPackage *WebExperience* | Remove-AppxPackage -AllUsers\"");
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", 0, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowWidgets", 0, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Chat", "ChatIcon", 3, RegistryValueKind.DWord);
                },
                UndoAction = () =>
                {
                    RunCommand("powershell.exe", "-NoProfile -Command \"$pkg = Get-AppxPackage -AllUsers *WebExperience*; if ($pkg) { Add-AppxPackage -DisableDevelopmentMode -Register \\\"$($pkg.InstallLocation)\\AppXManifest.xml\\\" } else { winget install 9MSSGKG348SP --accept-source-agreements --accept-package-agreements }\"");
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", 1, RegistryValueKind.DWord);
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests");
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowWidgets");
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Chat", "ChatIcon");
                },
            });

            // --- WINDOWS UPDATES ---
            Tweaks.Add(new TweakItem
            {
                Id = "PauseUpdates",
                Name = "Pause Windows Updates (10 Years)",
                Description = "Pauses quality and feature updates for 10 years, and disables the Update service. Can be undone anytime.",
                Category = "System",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    string nowStr = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string futureStr = DateTime.UtcNow.AddYears(10).ToString("yyyy-MM-ddTHH:mm:ssZ");

                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseUpdatesStartTime", nowStr, RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseUpdatesExpiryTime", futureStr, RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseFeatureUpdatesStartTime", nowStr, RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseFeatureUpdatesEndTime", futureStr, RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseQualityUpdatesStartTime", nowStr, RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseQualityUpdatesEndTime", futureStr, RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "FlightSettingsMaxPauseDays", 3652, RegistryValueKind.DWord);

                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1, RegistryValueKind.DWord);

                    RunCommand("sc.exe", "config wuauserv start= disabled");
                    RunCommand("sc.exe", "stop wuauserv");
                },
                UndoAction = () =>
                {
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseUpdatesStartTime");
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseUpdatesExpiryTime");
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseFeatureUpdatesStartTime");
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseFeatureUpdatesEndTime");
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseQualityUpdatesStartTime");
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseQualityUpdatesEndTime");
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "FlightSettingsMaxPauseDays");

                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate");

                    RunCommand("sc.exe", "config wuauserv start= auto");
                    RunCommand("sc.exe", "start wuauserv");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseUpdatesExpiryTime") != null
            });

            // --- BACKGROUND SERVICES ---
            Tweaks.Add(new TweakItem
            {
                Id = "ServiceFax",
                Name = "Disable Fax Service",
                Description = "Disables background fax transmission and reception processes (Safe for home computers).",
                Category = "Services",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config Fax start= disabled");
                    RunCommand("sc.exe", "stop Fax");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config Fax start= demand");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Fax", "Start") is int val && val == 4
            });

            Tweaks.Add(new TweakItem
            {
                Id = "ServiceRemoteRegistry",
                Name = "Disable Remote Registry",
                Description = "Disables remote modification of registry keys to improve system security (Highly Recommended).",
                Category = "Services",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config RemoteRegistry start= disabled");
                    RunCommand("sc.exe", "stop RemoteRegistry");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config RemoteRegistry start= demand");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\RemoteRegistry", "Start") is int val && val == 4
            });

            Tweaks.Add(new TweakItem
            {
                Id = "ServiceBluetooth",
                Name = "Disable Bluetooth Service",
                Description = "Disables background Bluetooth discoverability and support. (Caution: Turn off only if you do not use Bluetooth).",
                Category = "Services",
                Risk = "Caution",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config bthserv start= disabled");
                    RunCommand("sc.exe", "stop bthserv");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config bthserv start= demand");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\bthserv", "Start") is int val && val == 4
            });

            Tweaks.Add(new TweakItem
            {
                Id = "ServiceMapsBroker",
                Name = "Disable Downloaded Maps Manager",
                Description = "Disables background synchronization and caching of offline maps, saving CPU resources.",
                Category = "Services",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config MapsBroker start= disabled");
                    RunCommand("sc.exe", "stop MapsBroker");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config MapsBroker start= auto");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\MapsBroker", "Start") is int val && val == 4
            });

            Tweaks.Add(new TweakItem
            {
                Id = "ServicePrintSpooler",
                Name = "Disable Print Spooler",
                Description = "Disables background printing queue processes. (Caution: Turn off only if you do not have/use a physical printer).",
                Category = "Services",
                Risk = "Caution",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config Spooler start= disabled");
                    RunCommand("sc.exe", "stop Spooler");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config Spooler start= auto");
                    RunCommand("sc.exe", "start Spooler");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Spooler", "Start") is int val && val == 4
            });

            Tweaks.Add(new TweakItem
            {
                Id = "ServiceErrorReporting",
                Name = "Disable Windows Error Reporting",
                Description = "Disables logging and crash reporting uploads to Microsoft servers.",
                Category = "Services",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config WerSvc start= disabled");
                    RunCommand("sc.exe", "stop WerSvc");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config WerSvc start= demand");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\WerSvc", "Start") is int val && val == 4
            });

            // --- PERFORMANCE ---
            Tweaks.Add(new TweakItem
            {
                Id = "UltimatePower",
                Name = "Enable Ultimate Performance Plan",
                Description = "Unlocks the hidden Windows Ultimate Performance scheme to maximize CPU and hardware capability.",
                Category = "Performance",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    string output = RunCommand("powercfg.exe", "/duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                    string guid = "e9a42b02-d5df-448d-aa00-03f14749eb61";
                    if (output.Contains("GUID:"))
                    {
                        var parts = output.Split(new[] { "GUID:" }, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            guid = parts[1].Trim().Split(' ')[0];
                        }
                    }
                    RunCommand("powercfg.exe", $"/setactive {guid}");
                },
                UndoAction = () =>
                {
                    RunCommand("powercfg.exe", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                },
                CheckAppliedAction = () => RunCommand("powercfg.exe", "/getactivescheme").Contains("Ultimate Performance")
            });

            Tweaks.Add(new TweakItem
            {
                Id = "Hibernation",
                Name = "Disable Hibernation (Frees GBs of space)",
                Description = "Disables system hibernation and deletes the massive hiberfil.sys file (saves 4GB–16GB storage).",
                Category = "Performance",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    RunCommand("powercfg.exe", "/hibernate off");
                },
                UndoAction = () =>
                {
                    RunCommand("powercfg.exe", "/hibernate on");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\Power", "HibernateEnabled") is int val && val == 0
            });

            Tweaks.Add(new TweakItem
            {
                Id = "MouseAcceleration",
                Name = "Disable Mouse Acceleration",
                Description = "Turns off 'Enhance Pointer Precision' for raw, 1-to-1 mouse movements (ideal for gaming).",
                Category = "Gaming",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0", RegistryValueKind.String);
                    UpdateMouseSettings(0, 0, 0);
                },
                UndoAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "1", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "6", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "10", RegistryValueKind.String);
                    UpdateMouseSettings(6, 10, 1);
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed")?.ToString() == "0"
            });

            // --- SYSTEM SERVICES (ADVANCED) ---
            Tweaks.Add(new TweakItem
            {
                Id = "FaxService",
                Name = "Disable Fax Service",
                Description = "Disables the obsolete system service for sending and receiving faxes.",
                Category = "Services",
                Risk = "Advanced",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config Fax start= disabled");
                    RunCommand("sc.exe", "stop Fax");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config Fax start= demand");
                },
                CheckAppliedAction = () => RunCommand("sc.exe", "qc Fax").Contains("DISABLED")
            });

            Tweaks.Add(new TweakItem
            {
                Id = "RemoteRegistry",
                Name = "Disable Remote Registry Service",
                Description = "Prevents remote users from modifying your Windows Registry (recommended security tweak).",
                Category = "Services",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config RemoteRegistry start= disabled");
                    RunCommand("sc.exe", "stop RemoteRegistry");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config RemoteRegistry start= demand");
                },
                CheckAppliedAction = () => RunCommand("sc.exe", "qc RemoteRegistry").Contains("DISABLED")
            });

            // --- ADVANCED OPTIONS & UTILITIES ---
            Tweaks.Add(new TweakItem
            {
                Id = "TakeOwnership",
                Name = "Add 'Take Ownership' Context Menu",
                Description = "Adds a right-click 'Take Ownership' option to quickly gain administrative control of files/folders.",
                Category = "Customization",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    SetRegistryValue(RegistryHive.ClassesRoot, @"*\shell\runas_takeownership", "", "Take Ownership", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.ClassesRoot, @"*\shell\runas_takeownership", "NoWorkingDirectory", "", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.ClassesRoot, @"*\shell\runas_takeownership\command", "", "cmd.exe /c takeown /f \"%1\" && icacls \"%1\" /grant administrators:F", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.ClassesRoot, @"*\shell\runas_takeownership\command", "IsolatedCommand", "cmd.exe /c takeown /f \"%1\" && icacls \"%1\" /grant administrators:F", RegistryValueKind.String);
                    
                    SetRegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas_takeownership", "", "Take Ownership", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas_takeownership", "NoWorkingDirectory", "", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas_takeownership\command", "", "cmd.exe /c takeown /f \"%1\" /r /d y && icacls \"%1\" /grant administrators:F /t", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.ClassesRoot, @"Directory\shell\runas_takeownership\command", "IsolatedCommand", "cmd.exe /c takeown /f \"%1\" /r /d y && icacls \"%1\" /grant administrators:F /t", RegistryValueKind.String);
                },
                UndoAction = () =>
                {
                    DeleteRegistryKey(RegistryHive.ClassesRoot, @"*\shell\runas_takeownership");
                    DeleteRegistryKey(RegistryHive.ClassesRoot, @"Directory\shell\runas_takeownership");
                },
                CheckAppliedAction = () => RegistryKeyExists(RegistryHive.ClassesRoot, @"*\shell\runas_takeownership")
            });

            Tweaks.Add(new TweakItem
            {
                Id = "PrintSpoolerService",
                Name = "Disable Print Spooler Service",
                Description = "Disables the background printing service. Apply only if you do not use physical printers.",
                Category = "Services",
                Risk = "Advanced",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config Spooler start= disabled");
                    RunCommand("sc.exe", "stop Spooler");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config Spooler start= auto");
                },
                CheckAppliedAction = () => RunCommand("sc.exe", "qc Spooler").Contains("DISABLED")
            });

            Tweaks.Add(new TweakItem
            {
                Id = "BluetoothService",
                Name = "Disable Bluetooth Support Service",
                Description = "Disables Bluetooth wireless stack. Apply only if your PC does not use Bluetooth devices.",
                Category = "Services",
                Risk = "Advanced",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config bthserv start= disabled");
                    RunCommand("sc.exe", "stop bthserv");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config bthserv start= demand");
                },
                CheckAppliedAction = () => RunCommand("sc.exe", "qc bthserv").Contains("DISABLED")
            });

            Tweaks.Add(new TweakItem
            {
                Id = "SearchIndexerService",
                Name = "Disable Windows Search Indexer",
                Description = "Disables Windows background content search indexing, saving continuous CPU and disk overhead.",
                Category = "Services",
                Risk = "Advanced",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config WSearch start= disabled");
                    RunCommand("sc.exe", "stop WSearch");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config WSearch start= delayed-auto");
                },
                CheckAppliedAction = () => RunCommand("sc.exe", "qc WSearch").Contains("DISABLED")
            });

            Tweaks.Add(new TweakItem
            {
                Id = "SysMainService",
                Name = "Disable SysMain (Superfetch) Service",
                Description = "Disables the SysMain index caching service which can sometimes cause high memory spikes on SSD drives.",
                Category = "Services",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    RunCommand("sc.exe", "config SysMain start= disabled");
                    RunCommand("sc.exe", "stop SysMain");
                },
                UndoAction = () =>
                {
                    RunCommand("sc.exe", "config SysMain start= auto");
                },
                CheckAppliedAction = () => RunCommand("sc.exe", "qc SysMain").Contains("DISABLED")
            });

            Tweaks.Add(new TweakItem
            {
                Id = "TelemetryHostsBlock",
                Name = "Block Telemetry via DNS (Hosts File)",
                Description = "Redirects known Microsoft telemetry and tracking servers to 0.0.0.0 at the DNS level.",
                Category = "Privacy",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    BlockTelemetryHosts();
                },
                UndoAction = () =>
                {
                    UnblockTelemetryHosts();
                },
                CheckAppliedAction = () =>
                {
                    try
                    {
                        string hosts = File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts"));
                        return hosts.Contains("vortex.data.microsoft.com");
                    }
                    catch
                    {
                        return false;
                    }
                }
            });

            Tweaks.Add(new TweakItem
            {
                Id = "VisualFXTweak",
                Name = "Optimize Visual FX (Best Performance)",
                Description = "Disables resource-heavy system animations, shadows, and transition effects for maximum speed.",
                Category = "Customization",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0", RegistryValueKind.String);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 0, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Desktop", "UserPreferencesMask", new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
                    RunCommand("rundll32.exe", "user32.dll,UpdatePerUserSystemParameters 1 True");
                },
                UndoAction = () =>
                {
                    DeleteRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting");
                    SetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "1", RegistryValueKind.String);
                    DeleteRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations");
                    SetRegistryValue(RegistryHive.CurrentUser, @"Control Panel\Desktop", "UserPreferencesMask", new byte[] { 0xDE, 0x7E, 0x07, 0x80, 0x12, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
                    RunCommand("rundll32.exe", "user32.dll,UpdatePerUserSystemParameters 1 True");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting") is int val && val == 2
            });

            Tweaks.Add(new TweakItem
            {
                Id = "XboxGameDVR",
                Name = "Disable Xbox Live & Game DVR",
                Description = "Disables background Xbox Live network services, Game DVR screen capture overlays, and broadcasting to save CPU resources.",
                Category = "Gaming",
                Risk = "Safe",
                ApplyAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"System\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0, RegistryValueKind.DWord);
                    RunCommand("sc.exe", "config XblAuthManager start= disabled");
                    RunCommand("sc.exe", "stop XblAuthManager");
                    RunCommand("sc.exe", "config XblGameSave start= disabled");
                    RunCommand("sc.exe", "stop XblGameSave");
                    RunCommand("sc.exe", "config XboxNetApiSvc start= disabled");
                    RunCommand("sc.exe", "stop XboxNetApiSvc");
                },
                UndoAction = () =>
                {
                    SetRegistryValue(RegistryHive.CurrentUser, @"System\GameDVR", "AppCaptureEnabled", 1, RegistryValueKind.DWord);
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR");
                    RunCommand("sc.exe", "config XblAuthManager start= demand");
                    RunCommand("sc.exe", "config XblGameSave start= demand");
                    RunCommand("sc.exe", "config XboxNetApiSvc start= demand");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.CurrentUser, @"System\GameDVR", "AppCaptureEnabled") is int val && val == 0
            });

            Tweaks.Add(new TweakItem
            {
                Id = "DisableDefender",
                Name = "Disable Windows Defender Protection",
                Description = "Disables real-time monitoring and anti-spyware engines. (Caution: Apply only if you have third-party security software installed).",
                Category = "Privacy",
                Risk = "Caution",
                ApplyAction = () =>
                {
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableRealtimeMonitoring", 1, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableBehaviorMonitoring", 1, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableOnAccessProtection", 1, RegistryValueKind.DWord);
                    SetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableScanOnRealtimeEnable", 1, RegistryValueKind.DWord);
                },
                UndoAction = () =>
                {
                    DeleteRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware");
                    DeleteRegistryKey(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection");
                },
                CheckAppliedAction = () => GetRegistryValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware") is int val && val == 1
            });
        }

        public void CheckAllAppliedStates()
        {
            foreach (var tweak in Tweaks)
            {
                if (tweak.CheckAppliedAction != null)
                {
                    try
                    {
                        tweak.IsApplied = tweak.CheckAppliedAction();
                    }
                    catch
                    {
                        tweak.IsApplied = false;
                    }
                }
            }
        }

        // --- BACKEND API LOGIC ---



        public static Task<long> CleanTemporaryFilesAsync(Action<string> logCallback)
        {
            return Task.Run(() =>
            {
                long bytesFreed = 0;

                string userTemp = Path.GetTempPath();
                string systemTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                string updateDownload = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");

                logCallback("Starting Disk Cleanup...\n");

                bytesFreed += CleanDirectory(userTemp, logCallback);
                bytesFreed += CleanDirectory(systemTemp, logCallback);
                bytesFreed += CleanDirectory(prefetch, logCallback);
                bytesFreed += CleanDirectory(updateDownload, logCallback);

                logCallback("Flushing DNS Cache...\n");
                RunCommand("ipconfig.exe", "/flushdns");

                logCallback("Cleaning Windows Error Reporting logs...\n");
                string werPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "WER");
                if (Directory.Exists(werPath))
                {
                    bytesFreed += CleanDirectory(werPath, logCallback);
                }

                logCallback($"\nCleanup Completed! Freed {(bytesFreed / (1024.0 * 1024.0)):F2} MB.\n");
                return bytesFreed;
            });
        }

        private static long CleanDirectory(string path, Action<string> logCallback)
        {
            long freed = 0;
            if (!Directory.Exists(path)) return 0;

            logCallback($"Cleaning: {path}...\n");
            
            // Clean Files
            try
            {
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        long size = info.Length;
                        File.Delete(file);
                        freed += size;
                    }
                    catch
                    {
                        // File locked or inaccessible, skip silently
                    }
                }
            }
            catch (Exception ex)
            {
                logCallback($"Failed to read files in {path}: {ex.Message}\n");
            }

            // Clean Subdirectories
            try
            {
                var subDirs = Directory.GetDirectories(path);
                foreach (var dir in subDirs)
                {
                    try
                    {
                        long size = GetDirectorySize(dir);
                        Directory.Delete(dir, true);
                        freed += size;
                    }
                    catch
                    {
                        // Directory locked, skip silently
                    }
                }
            }
            catch (Exception ex)
            {
                logCallback($"Failed to read subdirectories in {path}: {ex.Message}\n");
            }

            return freed;
        }

        private static long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch { }
                }
            }
            catch { }
            return size;
        }

        // --- APPX MANAGEMENT ---

        public static Task<List<AppxPackageItem>> GetAppxPackagesAsync()
        {
            return Task.Run(() =>
            {
                var command = "Get-AppxPackage | Select-Object -Property Name, Version, InstallLocation | ConvertTo-Json";
                string json = RunPowerShellCommand(command);
                var apps = new List<AppxPackageItem>();

                // Parse each JSON object block {...}
                var blocks = System.Text.RegularExpressions.Regex.Matches(json, @"\{[^{}]*\}");
                foreach (System.Text.RegularExpressions.Match block in blocks)
                {
                    string blockText = block.Value;
                    string name = System.Text.RegularExpressions.Regex.Match(blockText, @"""Name"":\s*""([^""]+)""").Groups[1].Value;
                    string version = System.Text.RegularExpressions.Regex.Match(blockText, @"""Version"":\s*""([^""]+)""").Groups[1].Value;
                    string installLocation = System.Text.RegularExpressions.Regex.Match(blockText, @"""InstallLocation"":\s*""([^""]+)""").Groups[1].Value;

                    if (!string.IsNullOrEmpty(name) && !apps.Any(x => x.Name == name))
                    {
                        bool isBloat = IsBloatware(name);
                        installLocation = installLocation.Replace("\\\\", "\\");

                        string sizeStr = "0.0 MB";
                        try
                        {
                            if (!string.IsNullOrEmpty(installLocation) && System.IO.Directory.Exists(installLocation))
                            {
                                long sizeBytes = 0;
                                var files = System.IO.Directory.GetFiles(installLocation, "*", System.IO.SearchOption.AllDirectories);
                                foreach (var file in files)
                                {
                                    sizeBytes += new System.IO.FileInfo(file).Length;
                                }
                                sizeStr = $"{(sizeBytes / (1024.0 * 1024.0)):F1} MB";
                            }
                            else
                            {
                                sizeStr = "N/A";
                            }
                        }
                        catch
                        {
                            sizeStr = "Unknown";
                        }

                        apps.Add(new AppxPackageItem
                        {
                            Name = name,
                            DisplayName = GetFriendlyName(name),
                            Version = version,
                            IsRecommended = isBloat,
                            IsChecked = isBloat,
                            SizeMb = sizeStr,
                            IsDesktopApp = false
                        });
                    }
                }

                return apps.OrderByDescending(x => x.IsRecommended).ThenBy(x => x.DisplayName).ToList();
            });
        }

        public static Task<List<AppxPackageItem>> GetDesktopApplicationsAsync()
        {
            return Task.Run(() =>
            {
                var apps = new List<AppxPackageItem>();
                
                // Read 64-bit Registry Uninstall path
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subkeyName in key.GetSubKeyNames())
                        {
                            using (var subkey = key.OpenSubKey(subkeyName))
                            {
                                if (subkey == null) continue;
                                string displayName = subkey.GetValue("DisplayName") as string;
                                string displayVersion = subkey.GetValue("DisplayVersion") as string ?? "1.0";
                                string systemComponent = subkey.GetValue("SystemComponent")?.ToString();
                                string parentKeyName = subkey.GetValue("ParentKeyName") as string;
                                string uninstallString = subkey.GetValue("UninstallString") as string;
                                string quietUninstallString = subkey.GetValue("QuietUninstallString") as string;
                                
                                if (!string.IsNullOrEmpty(displayName) && systemComponent != "1" && string.IsNullOrEmpty(parentKeyName))
                                {
                                    if (!apps.Any(x => x.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        apps.Add(new AppxPackageItem
                                        {
                                            Name = subkeyName,
                                            DisplayName = displayName,
                                            Version = displayVersion,
                                            IsRecommended = false,
                                            IsChecked = false,
                                            SizeMb = "Desktop App",
                                            UninstallString = !string.IsNullOrEmpty(quietUninstallString) ? quietUninstallString : uninstallString,
                                            IsDesktopApp = true
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                // Read 32-bit Registry Uninstall path (Wow6432Node)
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                using (var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subkeyName in key.GetSubKeyNames())
                        {
                            using (var subkey = key.OpenSubKey(subkeyName))
                            {
                                if (subkey == null) continue;
                                string displayName = subkey.GetValue("DisplayName") as string;
                                string displayVersion = subkey.GetValue("DisplayVersion") as string ?? "1.0";
                                string systemComponent = subkey.GetValue("SystemComponent")?.ToString();
                                string parentKeyName = subkey.GetValue("ParentKeyName") as string;
                                string uninstallString = subkey.GetValue("UninstallString") as string;
                                string quietUninstallString = subkey.GetValue("QuietUninstallString") as string;

                                if (!string.IsNullOrEmpty(displayName) && systemComponent != "1" && string.IsNullOrEmpty(parentKeyName))
                                {
                                    if (!apps.Any(x => x.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        apps.Add(new AppxPackageItem
                                        {
                                            Name = subkeyName,
                                            DisplayName = displayName,
                                            Version = displayVersion,
                                            IsRecommended = false,
                                            IsChecked = false,
                                            SizeMb = "Desktop App",
                                            UninstallString = !string.IsNullOrEmpty(quietUninstallString) ? quietUninstallString : uninstallString,
                                            IsDesktopApp = true
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                // Read Current User Registry Uninstall path
                using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
                using (var key = hkcu.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subkeyName in key.GetSubKeyNames())
                        {
                            using (var subkey = key.OpenSubKey(subkeyName))
                            {
                                if (subkey == null) continue;
                                string displayName = subkey.GetValue("DisplayName") as string;
                                string displayVersion = subkey.GetValue("DisplayVersion") as string ?? "1.0";
                                string systemComponent = subkey.GetValue("SystemComponent")?.ToString();
                                string uninstallString = subkey.GetValue("UninstallString") as string;
                                string quietUninstallString = subkey.GetValue("QuietUninstallString") as string;
                                
                                if (!string.IsNullOrEmpty(displayName) && systemComponent != "1")
                                {
                                    if (!apps.Any(x => x.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        apps.Add(new AppxPackageItem
                                        {
                                            Name = subkeyName,
                                            DisplayName = displayName,
                                            Version = displayVersion,
                                            IsRecommended = false,
                                            IsChecked = false,
                                            SizeMb = "User App",
                                            UninstallString = !string.IsNullOrEmpty(quietUninstallString) ? quietUninstallString : uninstallString,
                                            IsDesktopApp = true
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                return apps.OrderBy(x => x.DisplayName).ToList();
            });
        }

        public static Task RemoveDesktopAppAsync(string name, string uninstallString, Action<string> logCallback)
        {
            return Task.Run(() =>
            {
                logCallback($"Uninstalling {name}...\n");
                if (string.IsNullOrEmpty(uninstallString))
                {
                    logCallback($"Error: No uninstall string found for {name}.\n");
                    return;
                }

                try
                {
                    if (uninstallString.Contains("MsiExec.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        string args = uninstallString.Replace("MsiExec.exe", "", StringComparison.OrdinalIgnoreCase).Trim();
                        args = args.Replace("/I", "/X", StringComparison.OrdinalIgnoreCase);
                        if (!args.Contains("/qn", StringComparison.OrdinalIgnoreCase))
                        {
                            args += " /qn /norestart";
                        }
                        logCallback($"Running MSI uninstaller: msiexec.exe {args}\n");
                        RunCommand("msiexec.exe", args);
                    }
                    else
                    {
                        logCallback($"Running uninstaller command: {uninstallString}\n");
                        string exe = "";
                        string args = "";
                        
                        if (uninstallString.StartsWith("\""))
                        {
                            int nextQuote = uninstallString.IndexOf("\"", 1);
                            if (nextQuote > 0)
                            {
                                exe = uninstallString.Substring(1, nextQuote - 1);
                                args = uninstallString.Substring(nextQuote + 1).Trim();
                            }
                        }
                        else
                        {
                            int space = uninstallString.IndexOf(" ");
                            if (space > 0)
                            {
                                exe = uninstallString.Substring(0, space);
                                args = uninstallString.Substring(space + 1).Trim();
                            }
                            else
                            {
                                exe = uninstallString;
                            }
                        }

                        if (uninstallString.Contains("Inno Setup", StringComparison.OrdinalIgnoreCase) || uninstallString.Contains("unins000", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!args.Contains("/SILENT", StringComparison.OrdinalIgnoreCase))
                                args += " /VERYSILENT /SUPPRESSMSGBOXES /NORESTART";
                        }
                        else if (uninstallString.Contains("InstallShield", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!args.Contains("/s", StringComparison.OrdinalIgnoreCase))
                                args += " /s /v\"/qn\"";
                        }
                        else if (uninstallString.Contains("Nullsoft", StringComparison.OrdinalIgnoreCase) || uninstallString.Contains("NSIS", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!args.Contains("/S", StringComparison.OrdinalIgnoreCase))
                                args += " /S";
                        }

                        var processInfo = new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = args,
                            UseShellExecute = true,
                            CreateNoWindow = false
                        };
                        using var process = Process.Start(processInfo);
                        process?.WaitForExit();
                    }
                    logCallback($"{name} uninstalled successfully.\n");
                }
                catch (Exception ex)
                {
                    logCallback($"Error uninstalling {name}: {ex.Message}\n");
                }
            });
        }



        private static string GetFriendlyName(string name)
        {
            string friendly = name;
            if (friendly.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
            {
                friendly = friendly.Substring("Microsoft.".Length);
            }
            if (friendly.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase))
            {
                friendly = friendly.Substring("Windows.".Length);
            }
            return friendly;
        }

        private static bool IsBloatware(string name)
        {
            string[] bloatKeywords = {
                "Cortana", "Xbox", "Bing", "Skype", "Zune", "GetHelp", "FeedbackHub",
                "YourPhone", "StickyNotes", "OneNote", "OfficeHub", "SolitaireCollection",
                "MixedReality", "People", "Wallet", "Maps", "Weather", "Spotify", "Disney"
            };

            return bloatKeywords.Any(keyword => name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        public static Task RemoveAppxPackageAsync(string packageName, Action<string> logCallback)
        {
            return Task.Run(() =>
            {
                logCallback($"Removing Appx Package: {packageName}...\n");
                // Remove for all users
                RunPowerShellCommand($"Get-AppxPackage -Name '{packageName}' -AllUsers | Remove-AppxPackage -ErrorAction SilentlyContinue");
                // Remove provisioned package so it doesn't return on new users
                RunPowerShellCommand($"Get-AppxProvisionedPackage -Online | Where-Object {{$_.DisplayName -eq '{packageName}'}} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue");
                logCallback($"Successfully uninstalled {packageName}.\n");
            });
        }

        // --- WINGET SOFTWARE INSTALLER ---

        public static Task InstallSoftwareAsync(string wingetId, Action<string> logCallback)
        {
            return Task.Run(() =>
            {
                logCallback($"Installing {wingetId} via winget. Please wait...\n");
                string args = $"install --id {wingetId} --silent --accept-source-agreements --accept-package-agreements";
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "winget.exe",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        string? line = process.StandardOutput.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            logCallback($"{line}\n");
                        }
                    }
                    process.WaitForExit();
                    logCallback($"\nWinget installation finished for {wingetId} with exit code: {process.ExitCode}\n");
                }
                else
                {
                    logCallback("Failed to start winget.exe. Make sure winget is installed and configured on your PATH.\n");
                }
            });
        }

        public static Task<List<SoftwareItem>> SearchWingetAsync(string query)
        {
            return Task.Run(() =>
            {
                var list = new List<SoftwareItem>();
                if (string.IsNullOrWhiteSpace(query)) return list;

                // Sanitize input to prevent shell injection (letters, numbers, space, dot, hyphen only)
                query = System.Text.RegularExpressions.Regex.Replace(query, @"[^a-zA-Z0-9\.\-\s]", "");
                if (string.IsNullOrWhiteSpace(query)) return list;

                string output = RunCommand("winget.exe", $"search \"{query}\"");
                
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool startParsing = false;
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
                        if (id.Contains(".") && !list.Any(x => x.Id == id))
                        {
                            list.Add(new SoftwareItem { Name = name, Id = id });
                        }
                    }
                }
                return list.Take(12).ToList();
            });
        }

        // --- SYSTEM REPAIR UTILITIES ---

        public static Task RunSystemRepairAsync(string tool, Action<string> logCallback)
        {
            return Task.Run(() =>
            {
                string exe = "cmd.exe";
                string args = "";

                if (tool == "SFC")
                {
                    logCallback("Running System File Checker (sfc /scannow)...\nThis may take several minutes.\n\n");
                    args = "/c sfc /scannow";
                }
                else if (tool == "DISM")
                {
                    logCallback("Running DISM System Image Restore...\nThis may take several minutes.\n\n");
                    args = "/c DISM /Online /Cleanup-Image /RestoreHealth";
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        string? line = process.StandardOutput.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            logCallback($"{line}\n");
                        }
                    }
                    process.WaitForExit();
                    logCallback($"\nSystem maintenance tool completed with code: {process.ExitCode}\n");
                }
            });
        }

        // --- HELPER UTILITIES ---

        public static Task CreateRestorePointAsync(Action<string> logCallback)
        {
            return Task.Run(() =>
            {
                try
                {
                    string systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                    RunPowerShellCommand($"Enable-ComputerRestore -Drive \"{systemDrive}\" -ErrorAction SilentlyContinue");
                    
                    logCallback("Creating Windows System Restore Point 'ApexDebloater Backup'...\nThis may take up to a minute.\n");
                    string output = RunPowerShellCommand("Checkpoint-Computer -Description 'ApexDebloater Backup' -RestorePointType MODIFY_SETTINGS");
                    if (string.IsNullOrEmpty(output) || !output.Contains("Error"))
                    {
                        logCallback("System Restore Point created successfully.\n");
                    }
                    else
                    {
                        logCallback($"System Restore Point status: {output.Trim()}\n");
                    }
                }
                catch (Exception ex)
                {
                    logCallback($"Warning: System Restore point could not be created: {ex.Message}\n");
                }
            });
        }

        public static Task RestoreRegistryBackupsAsync(Action<string> logCallback)
        {
            return Task.Run(() =>
            {
                string backupDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!System.IO.Directory.Exists(backupDir))
                {
                    logCallback("No registry backups folder found. Nothing to restore.\n");
                    return;
                }

                var regFiles = System.IO.Directory.GetFiles(backupDir, "*.reg");
                if (regFiles.Length == 0)
                {
                    logCallback("No registry backup files found to restore.\n");
                    return;
                }

                logCallback($"Restoring {regFiles.Length} registry backups...\n");
                int successCount = 0;
                foreach (var file in regFiles)
                {
                    string output = RunCommand("reg.exe", $"import \"{file}\"");
                    // Reg import output might be empty or localized, so checking process status is better
                    successCount++;
                }
                logCallback($"Restored registry backup files.\n");
                
                logCallback("Restarting Windows Explorer to apply changes...\n");
                RestartExplorer();
                logCallback("System restore complete!\n");
            });
        }

        private static void BackupKey(RegistryHive hive, string keyPath)
        {
            try
            {
                string hiveStr = hive switch
                {
                    RegistryHive.CurrentUser => "HKCU",
                    RegistryHive.LocalMachine => "HKLM",
                    RegistryHive.ClassesRoot => "HKCR",
                    RegistryHive.Users => "HKU",
                    _ => ""
                };

                if (string.IsNullOrEmpty(hiveStr)) return;

                string backupDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!System.IO.Directory.Exists(backupDir))
                {
                    System.IO.Directory.CreateDirectory(backupDir);
                }

                string safeName = $"{hiveStr}_{keyPath.Replace("\\", "_")}.reg";
                string backupFile = System.IO.Path.Combine(backupDir, safeName);

                // Run reg.exe export (overwrite existing backup of same key)
                RunCommand("reg.exe", $"export \"{hiveStr}\\{keyPath}\" \"{backupFile}\" /y");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to backup registry key {keyPath}: {ex.Message}");
            }
        }

        public static object? GetRegistryValue(RegistryHive hive, string keyPath, string valueName)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(keyPath, false);
                return key?.GetValue(valueName);
            }
            catch
            {
                return null;
            }
        }

        public static bool RegistryKeyExists(RegistryHive hive, string keyPath)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(keyPath, false);
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        private static void SetRegistryValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind valueKind)
        {
            BackupKey(hive, keyPath);
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.CreateSubKey(keyPath);
                key.SetValue(valueName, value, valueKind);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting registry value {keyPath}\\{valueName}: {ex.Message}");
            }
        }

        private static void DeleteRegistryValue(RegistryHive hive, string keyPath, string valueName)
        {
            BackupKey(hive, keyPath);
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(keyPath, true);
                key?.DeleteValue(valueName, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting registry value {keyPath}\\{valueName}: {ex.Message}");
            }
        }

        private static void DeleteRegistryKey(RegistryHive hive, string keyPath)
        {
            BackupKey(hive, keyPath);
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                baseKey.DeleteSubKeyTree(keyPath, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting registry key {keyPath}: {ex.Message}");
            }
        }

        private static string RunCommand(string filename, string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error running command {filename} {arguments}: {ex.Message}");
            }
            return string.Empty;
        }

        public static string RunPowerShellCommand(string command)
        {
            // Escape single quotes inside commands
            string escapedCommand = command.Replace("\"", "\\\"");
            return RunCommand("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{escapedCommand}\"");
        }

        public static void RestartExplorer()
        {
            RunCommand("taskkill.exe", "/f /im explorer.exe");
            Process.Start("explorer.exe");
        }

        // --- DYNAMIC SYSTEM INFO HELPERS ---

        public static string GetSystemModel()
        {
            try
            {
                string model = RunPowerShellCommand("(Get-CimInstance Win32_ComputerSystem).Model").Trim();
                if (!string.IsNullOrEmpty(model)) return model;
            }
            catch {}
            return "Windows PC";
        }

        public static string GetSystemManufacturer()
        {
            try
            {
                string manufacturer = RunPowerShellCommand("(Get-CimInstance Win32_ComputerSystem).Manufacturer").Trim();
                if (!string.IsNullOrEmpty(manufacturer)) return manufacturer;
            }
            catch {}
            return "Generic";
        }

        public static string GetUserFullName()
        {
            try
            {
                string name = RunPowerShellCommand("([adsi]\"WinNT://$env:USERDOMAIN/$env:USERNAME,user\").FullName").Trim();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch {}
            return Environment.UserName;
        }

        public static string GetUserAccountType()
        {
            try
            {
                string email = RunPowerShellCommand("Get-ItemPropertyValue -Path 'HKCU:\\Software\\Microsoft\\IdentityCRL\\UserExtendedProperties\\*' -Name 'EmailAddress' -ErrorAction SilentlyContinue").Trim();
                if (!string.IsNullOrEmpty(email)) return email;
            }
            catch {}
            return $"{Environment.UserDomainName}\\{Environment.UserName}";
        }

        public static Task CreateRestorePointAsync()
        {
            return CreateRestorePointAsync((_) => {});
        }

        public static Task RestoreRegistryBackupsAsync()
        {
            return RestoreRegistryBackupsAsync((_) => {});
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uiAction, int uiParam, int[] pvParam, int fWinIni);

        private const int SPI_SETMOUSE = 0x0004;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        private static void UpdateMouseSettings(int threshold1, int threshold2, int speed)
        {
            try
            {
                int[] mouseParams = new[] { threshold1, threshold2, speed };
                SystemParametersInfo(SPI_SETMOUSE, 0, mouseParams, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply real-time mouse settings: {ex.Message}");
            }
        }

        [System.Runtime.InteropServices.DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hProcess);

        public static Task<long> FlushMemoryAsync(Action<string> logCallback)
        {
            return Task.Run(() =>
            {
                logCallback("Starting RAM standby cache flush...\n");
                long totalFreed = 0;
                var currentProcess = Process.GetCurrentProcess();

                foreach (var process in Process.GetProcesses())
                {
                    if (process.Id == 0 || process.Id == 4 || process.Id == currentProcess.Id)
                        continue;

                    try
                    {
                        long before = process.WorkingSet64;
                        if (EmptyWorkingSet(process.Handle) != 0)
                        {
                            long after = process.WorkingSet64;
                            if (before > after)
                            {
                                totalFreed += (before - after);
                            }
                        }
                    }
                    catch
                    {
                        // Skip if access denied or process terminated
                    }
                }

                logCallback("Successfully flushed system working set memory cache.\n");
                return totalFreed;
            });
        }

        // --- STARTUP MANAGER BACKEND ---
        public static Task<List<StartupItem>> GetStartupItemsAsync()
        {
            return Task.Run(() =>
            {
                var list = new List<StartupItem>();
                ScanRunKey(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", list);
                ScanRunKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", list);
                return list;
            });
        }

        private static void ScanRunKey(RegistryHive hive, string path, List<StartupItem> list)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(path, false);
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        object? val = key.GetValue(valueName);
                        if (val != null)
                        {
                            bool isEnabled = !valueName.StartsWith("Disabled_", StringComparison.OrdinalIgnoreCase);
                            string name = isEnabled ? valueName : valueName.Substring("Disabled_".Length);

                            list.Add(new StartupItem
                            {
                                Name = name,
                                Command = val.ToString() ?? string.Empty,
                                RegistryPath = (hive == RegistryHive.CurrentUser ? "HKCU\\" : "HKLM\\") + path + "\\" + valueName,
                                IsEnabled = isEnabled,
                                IsChecked = false
                            });
                        }
                    }
                }
            }
            catch {}
        }

        public static void ToggleStartupItem(StartupItem item, bool enable)
        {
            if (item.IsEnabled == enable) return;

            try
            {
                string fullPath = item.RegistryPath;
                RegistryHive hive = fullPath.StartsWith("HKCU") ? RegistryHive.CurrentUser : RegistryHive.LocalMachine;
                
                string pathAndVal = fullPath.Substring(5);
                int lastSlash = pathAndVal.LastIndexOf('\\');
                string keyPath = pathAndVal.Substring(0, lastSlash);
                string currentValueName = pathAndVal.Substring(lastSlash + 1);

                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(keyPath, true);
                if (key != null)
                {
                    object? val = key.GetValue(currentValueName);
                    if (val != null)
                    {
                        key.DeleteValue(currentValueName, false);

                        string newName = enable ? item.Name : "Disabled_" + item.Name;
                        key.SetValue(newName, val);

                        item.RegistryPath = (hive == RegistryHive.CurrentUser ? "HKCU\\" : "HKLM\\") + keyPath + "\\" + newName;
                        item.IsEnabled = enable;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to toggle startup item {item.Name}: {ex.Message}");
            }
        }

        // --- CONTEXT MENU CLEANER BACKEND ---
        public static Task<List<ShellExtensionItem>> GetShellExtensionsAsync()
        {
            return Task.Run(() =>
            {
                var list = new List<ShellExtensionItem>();
                ScanShellExtensionsKey(@"*\shellex\ContextMenuHandlers", list);
                ScanShellExtensionsKey(@"Directory\shellex\ContextMenuHandlers", list);
                return list;
            });
        }

        private static string ResolveGuidToFriendlyName(string keyOrGuid, RegistryKey baseKey, string relativePath, string subkeyName)
        {
            string targetGuid = keyOrGuid;
            
            // 1. If keyOrGuid is not a GUID, check if the default value of the subkey is a GUID, or use the default value if it's a friendly string
            if (!keyOrGuid.StartsWith("{"))
            {
                try
                {
                    using var subKey = baseKey.OpenSubKey(relativePath + "\\" + subkeyName, false);
                    object? val = subKey?.GetValue("");
                    if (val != null)
                    {
                        string valStr = val.ToString()!.Trim();
                        if (valStr.StartsWith("{"))
                        {
                            targetGuid = valStr;
                        }
                        else if (!string.IsNullOrWhiteSpace(valStr))
                        {
                            return valStr;
                        }
                    }
                }
                catch {}
            }

            // 2. Resolve GUID under HKEY_CLASSES_ROOT\CLSID
            if (targetGuid.StartsWith("{") && targetGuid.EndsWith("}"))
            {
                try
                {
                    using var clsidKey = baseKey.OpenSubKey(@"CLSID\" + targetGuid, false);
                    if (clsidKey != null)
                    {
                        object? friendlyNameVal = clsidKey.GetValue("");
                        if (friendlyNameVal != null && !string.IsNullOrWhiteSpace(friendlyNameVal.ToString()))
                        {
                            return friendlyNameVal.ToString()!.Trim();
                        }
                        
                        // Fallback to InprocServer32 DLL name
                        using var serverKey = clsidKey.OpenSubKey("InprocServer32", false);
                        object? pathVal = serverKey?.GetValue("");
                        if (pathVal != null)
                        {
                            string path = pathVal.ToString()!;
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                            if (!string.IsNullOrWhiteSpace(fileName))
                            {
                                return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fileName.ToLower()) + " Extension";
                            }
                        }
                    }
                }
                catch {}
            }

            return keyOrGuid;
        }

        private static void ScanShellExtensionsKey(string path, List<ShellExtensionItem> list)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(path, false);
                if (key != null)
                {
                    foreach (var subkeyName in key.GetSubKeyNames())
                    {
                        if (subkeyName.Equals("WorkFolders", StringComparison.OrdinalIgnoreCase) ||
                            subkeyName.Equals("OpenWith", StringComparison.OrdinalIgnoreCase) ||
                            subkeyName.Equals("Sharing", StringComparison.OrdinalIgnoreCase) ||
                            subkeyName.Equals("OpenWithAntiVirus", StringComparison.OrdinalIgnoreCase))
                            continue;

                        bool isEnabled = !subkeyName.StartsWith("-");
                        string rawKeyName = isEnabled ? subkeyName : subkeyName.TrimStart('-');
                        string friendlyName = ResolveGuidToFriendlyName(rawKeyName, baseKey, path, subkeyName);

                        list.Add(new ShellExtensionItem
                        {
                            Name = friendlyName,
                            RegistryKeyName = rawKeyName,
                            RegistryPath = @"HKCR\" + path + @"\" + subkeyName,
                            FriendlyDescription = $"[{rawKeyName}] at {path}",
                            IsEnabled = isEnabled,
                            IsChecked = false
                        });
                    }
                }
            }
            catch {}
        }

        public static void ToggleShellExtension(ShellExtensionItem item, bool enable)
        {
            if (item.IsEnabled == enable) return;

            try
            {
                string fullPath = item.RegistryPath;
                string path = fullPath.Substring(5);
                int lastSlash = path.LastIndexOf('\\');
                string parentKeyPath = path.Substring(0, lastSlash);
                string currentKeyName = path.Substring(lastSlash + 1);

                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
                string newKeyName = enable ? item.RegistryKeyName : "-" + item.RegistryKeyName;
                
                using var parentKey = baseKey.OpenSubKey(parentKeyPath, true);
                if (parentKey != null)
                {
                    using var oldKey = parentKey.OpenSubKey(currentKeyName);
                    if (oldKey != null)
                    {
                        object? defaultVal = oldKey.GetValue("");
                        parentKey.DeleteSubKeyTree(currentKeyName, false);
                        
                        using var newKey = parentKey.CreateSubKey(newKeyName);
                        if (defaultVal != null)
                        {
                            newKey.SetValue("", defaultVal);
                        }

                        item.RegistryPath = @"HKCR\" + parentKeyPath + @"\" + newKeyName;
                        item.IsEnabled = enable;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to toggle shell extension {item.Name}: {ex.Message}");
            }
        }

        // --- DNS TELEMETRY BLOCKER ---
        public static void BlockTelemetryHosts()
        {
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                if (File.Exists(hostsPath))
                {
                    var domains = new[]
                    {
                        "0.0.0.0 vortex.data.microsoft.com",
                        "0.0.0.0 settings-win.data.microsoft.com",
                        "0.0.0.0 watson.telemetry.microsoft.com",
                        "0.0.0.0 diagnostics.office.com",
                        "0.0.0.0 telemetry.microsoft.com"
                    };

                    var lines = File.ReadAllLines(hostsPath).ToList();
                    bool added = false;
                    foreach (var domain in domains)
                    {
                        if (!lines.Any(l => l.Trim().Equals(domain, StringComparison.OrdinalIgnoreCase)))
                        {
                            lines.Add(domain);
                            added = true;
                        }
                    }

                    if (added)
                    {
                        File.WriteAllLines(hostsPath, lines);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write hosts file: {ex.Message}");
            }
        }

        public static void UnblockTelemetryHosts()
        {
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                if (File.Exists(hostsPath))
                {
                    var domains = new[]
                    {
                        "vortex.data.microsoft.com",
                        "settings-win.data.microsoft.com",
                        "watson.telemetry.microsoft.com",
                        "diagnostics.office.com",
                        "telemetry.microsoft.com"
                    };

                    var lines = File.ReadAllLines(hostsPath).ToList();
                    var newLines = lines.Where(line => 
                        !domains.Any(d => line.Contains(d))
                    ).ToList();

                    File.WriteAllLines(hostsPath, newLines);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clean hosts file: {ex.Message}");
            }
        }
    }
}
