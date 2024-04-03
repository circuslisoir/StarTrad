using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace StarTrad.Tool
{
    internal class RsiLauncherFolder
    {
        private const string RSI_LAUNCHER_EXECUTABLE_FILE_NAME = "RSI Launcher.exe";
        private const string RSI_LAUNCHER_REGISTRY_KEY = "81bfc699-f883-50c7-b674-2483b6baae23";

        /*
        Static
        */

        /// <summary>
        /// Tries to execute the RSI Launcher, if we can find where it is installed.
        /// </summary>
        public static void ExecuteRsiLauncher()
        {
            string? folderPath = GetFolderPath(true);

            if (folderPath == null)
            {
                return;
            }

            string? exePath = folderPath + '\\' + RSI_LAUNCHER_EXECUTABLE_FILE_NAME;

            if (!File.Exists(exePath))
            {
                return;
            }

            Process.Start(exePath);

            // When the process handler is running it will already start the external processes as
            // soon as it detects that the RSI launcher has been started, so no need to do it manually.
            if (!ProcessHandler.IsProcessHandlerRunning)
            {
                ProcessHandler.StartExternalProcess();
            }
        }

        /// <summary>
        /// Gets the absolute path to the RSI Launcher folder either from the settings or by trying to find it again.
        /// <param name="useLibraryFolder">
        /// False to not use the path to the Library Folder for finding where the RSI Launcher is.
        /// Prevents a dependency loop where the RsiLauncherFolder depends on the Library Folder, itself depending on the RsiLauncherFolder.
        /// </param>
        /// </summary>
        /// <returns></returns>
		public static string? GetFolderPath(bool useLibraryFolder = false)
        {
            if (IsValidFolderPath(Properties.Settings.Default.RsiLauncherFolderPath))
            {
                return Properties.Settings.Default.RsiLauncherFolderPath;
            }

            string? rsiLauncherFolderPath = FindFolderPath(useLibraryFolder);

            if (rsiLauncherFolderPath == null)
            {
                return null;
            }

            Properties.Settings.Default.RsiLauncherFolderPath = rsiLauncherFolderPath;
            Properties.Settings.Default.Save();

            return rsiLauncherFolderPath;
        }

        /// <summary>
        /// Checks if the given path is a directory containing the RSI Launcher.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool IsValidFolderPath(string path)
        {
            return Directory.Exists(path)
                && File.Exists(path + '\\' + RSI_LAUNCHER_EXECUTABLE_FILE_NAME);
        }

        #region Rsi launcher folder path finding methods

        /// <summary>
        /// Tries to obtain the path to the RSI Launcher folder.
        /// </summary>
        /// <returns></returns>
        private static string? FindFolderPath(bool useLibraryFolder = false)
        {
            string? rsiLauncherFolderPath = null;

            if (useLibraryFolder)
            {
                // Try to find relative to the Library Folder
                rsiLauncherFolderPath = LibraryFolder.GetFolderPath() + @$"\{Properties.Settings.Default.LauncherName}";

                if (IsValidFolderPath(rsiLauncherFolderPath))
                {
                    return rsiLauncherFolderPath;
                }
            }

            // Try to find from the registry
            rsiLauncherFolderPath = FindFolderPathFromRegistry();

            if (rsiLauncherFolderPath != null)
            {
                return rsiLauncherFolderPath;
            }

            rsiLauncherFolderPath = FindFolderPathFromStartMenu();

            if (rsiLauncherFolderPath != null)
            {
                return rsiLauncherFolderPath;
            }

            rsiLauncherFolderPath = FindFolderPathFromDesktop();

            if (rsiLauncherFolderPath != null)
            {
                return rsiLauncherFolderPath;
            }

            return null;
        }

        /// <summary>
        /// Attemps to find where the RSI Launcher is installed by reading the registry.
        /// </summary>
        /// <returns>
        /// The absolute path to the RSI Launcher folder, or null if it cannot be found.
        /// </returns>
        private static string? FindFolderPathFromRegistry()
        {
            // 'C:\Program Files\Roberts Space Industries\RSI Launcher'
            string? installLocation = (string?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\{RSI_LAUNCHER_REGISTRY_KEY}", "InstallLocation", null);
            Logger.LogInformation("installLocation = '" + installLocation + "'");

            if (installLocation != null && IsValidFolderPath(installLocation))
            {
                return installLocation;
            }

            // 'C:\Program Files\Roberts Space Industries\RSI Launcher\uninstallerIcon.ico'
            string? displayIcon = (string?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{RSI_LAUNCHER_REGISTRY_KEY}", "DisplayIcon", null);
            Logger.LogInformation("displayIcon = '" + displayIcon + "'");

            if (displayIcon != null && File.Exists(displayIcon))
            {
                string? rsiLauncherFolderPath = Path.GetDirectoryName(displayIcon);

                if (rsiLauncherFolderPath != null && IsValidFolderPath(rsiLauncherFolderPath))
                {
                    return rsiLauncherFolderPath;
                }
            }

            // "C:\Program Files\Roberts Space Industries\RSI Launcher\Uninstall RSI Launcher.exe" /allusers /S
            string? quietUninstallString = (string?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{RSI_LAUNCHER_REGISTRY_KEY}", "QuietUninstallString", null);
            Logger.LogInformation("quietUninstallString = '" + quietUninstallString + "'");

            if (quietUninstallString != null)
            {
                int firstQuotePos = quietUninstallString.IndexOf('"');
                int secondQuotePos = quietUninstallString.IndexOf('"', firstQuotePos + 1);
                string? launcherUninstallerExecutablePath = null;

                try
                {
                    launcherUninstallerExecutablePath = quietUninstallString.Substring(firstQuotePos, secondQuotePos);
                }
                catch (ArgumentOutOfRangeException)
                {
                }

                if (launcherUninstallerExecutablePath != null)
                {
                    string? rsiLauncherFolderPath = Path.GetDirectoryName(displayIcon);

                    if (rsiLauncherFolderPath != null && IsValidFolderPath(rsiLauncherFolderPath))
                    {
                        return rsiLauncherFolderPath;
                    }
                }
            }

            // '"C:\Program Files\Roberts Space Industries\RSI Launcher\Uninstall RSI Launcher.exe" /allusers'
            string? uninstallString = (string?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{RSI_LAUNCHER_REGISTRY_KEY}", "UninstallString", null);
            Logger.LogInformation("uninstallString = '" + uninstallString + "'");

            if (uninstallString != null)
            {
                int firstQuotePos = uninstallString.IndexOf('"');
                int secondQuotePos = uninstallString.IndexOf('"', firstQuotePos + 1);
                string? launcherUninstallerExecutablePath = null;

                try
                {
                    launcherUninstallerExecutablePath = uninstallString.Substring(firstQuotePos, secondQuotePos);
                }
                catch (ArgumentOutOfRangeException)
                {
                }

                if (launcherUninstallerExecutablePath != null)
                {
                    string? rsiLauncherFolderPath = Path.GetDirectoryName(displayIcon);

                    if (rsiLauncherFolderPath != null && IsValidFolderPath(rsiLauncherFolderPath))
                    {
                        return rsiLauncherFolderPath;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Attemps to find where the RSI Launcher is installed by finding a shortcut to it in the Start Menu.
        /// </summary>
        /// <returns>
        /// The absolute path to the RSI Launcher folder, or null if it cannot be found.
        /// </returns>
        public static string? FindFolderPathFromStartMenu()
        {
            // "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Roberts Space Industries\RSI Launcher.lnk"
            string? rsiLauncherExePath = Shortcut.ReadTargetOfShortcut(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms) + @$"\Roberts Space Industries\{Properties.Settings.Default.LauncherName}.lnk");

            if (rsiLauncherExePath == null || !File.Exists(rsiLauncherExePath))
            {
                return null;
            }

            return Path.GetDirectoryName(rsiLauncherExePath);
        }

        /// <summary>
        /// Attemps to find where the RSI Launcher is installed by finding a shortcut to it on the Desktop.
        /// </summary>
        /// <returns>
        /// The absolute path to the RSI Launcher folder, or null if it cannot be found.
        /// </returns>
        public static string? FindFolderPathFromDesktop()
        {
            string? rsiLauncherExePath = Shortcut.ReadTargetOfShortcut(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory) + @$"\{Properties.Settings.Default.LauncherName}.lnk");

            if (rsiLauncherExePath != null && File.Exists(rsiLauncherExePath))
            {
                return Path.GetDirectoryName(rsiLauncherExePath);
            }

            // "C:\Users\Public\Desktop\RSI Launcher.lnk"
            rsiLauncherExePath = Shortcut.ReadTargetOfShortcut(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @$"\{Properties.Settings.Default.LauncherName}.lnk");

            if (rsiLauncherExePath != null && File.Exists(rsiLauncherExePath))
            {
                return Path.GetDirectoryName(rsiLauncherExePath);
            }

            return null;
        }

        #endregion
    }
}
