using Microsoft.Win32;
using StarTrad.Helper;
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
            string? folderPath = GetFolderPath();

            if (folderPath == null) {
                return;
            }
            
            string? exePath = folderPath + '\\' + RSI_LAUNCHER_EXECUTABLE_FILE_NAME;

            if (!File.Exists(exePath)) {
				return;
			}

            Process.Start(exePath);
        }

        /// <summary>
        /// Gets the absolute path to the RSI Launcher folder either from the settings or by trying to find it again.
        /// </summary>
        /// <returns></returns>
		public static string? GetFolderPath()
		{
            if (IsValidFolderPath(Properties.Settings.Default.RsiLauncherFolderPath)) {
                return Properties.Settings.Default.RsiLauncherFolderPath;
            }

            string? rsiLauncherFolderPath = FindFolderPath();

            if (rsiLauncherFolderPath == null) {
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
		private static string? FindFolderPath()
        {
            // Try to find relative to the Library Folder
            string? rsiLauncherFolderPath = LibraryFolder.GetFolderPath() + @"\RSI Launcher";

            if (IsValidFolderPath(rsiLauncherFolderPath)) {
                return rsiLauncherFolderPath;
            }

            // Try to find from the registry
            rsiLauncherFolderPath = FindFolderPathFromRegistry();

            if (rsiLauncherFolderPath != null) {
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
            LoggerFactory.LogInformation("installLocation = '" + installLocation + "'");

            if (installLocation != null && IsValidFolderPath(installLocation)) {
                 return installLocation;
            }

            // 'C:\Program Files\Roberts Space Industries\RSI Launcher\uninstallerIcon.ico'
            string? displayIcon = (string?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{RSI_LAUNCHER_REGISTRY_KEY}", "DisplayIcon", null);
            LoggerFactory.LogInformation("displayIcon = '" + displayIcon + "'");

            if (displayIcon != null && File.Exists(displayIcon)) {
                string? rsiLauncherFolderPath = Path.GetDirectoryName(displayIcon);

                if (rsiLauncherFolderPath != null && IsValidFolderPath(rsiLauncherFolderPath)) {
                    return rsiLauncherFolderPath;
                }
            }

            // "C:\Program Files\Roberts Space Industries\RSI Launcher\Uninstall RSI Launcher.exe" /allusers /S
            string? quietUninstallString = (string?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{RSI_LAUNCHER_REGISTRY_KEY}", "QuietUninstallString", null);
            LoggerFactory.LogInformation("quietUninstallString = '" + quietUninstallString + "'");

            if (quietUninstallString != null) {
                int firstQuotePos = quietUninstallString.IndexOf('"');
                int secondQuotePos = quietUninstallString.IndexOf('"', firstQuotePos+1);
                string? launcherUninstallerExecutablePath = null;

                try {
                    launcherUninstallerExecutablePath = quietUninstallString.Substring(firstQuotePos, secondQuotePos);
                } catch (ArgumentOutOfRangeException) {
                }

                if (launcherUninstallerExecutablePath != null) {
                    string? rsiLauncherFolderPath = Path.GetDirectoryName(displayIcon);

                    if (rsiLauncherFolderPath != null && IsValidFolderPath(rsiLauncherFolderPath)) {
                        return rsiLauncherFolderPath;
                    }
                }
            }

            // '"C:\Program Files\Roberts Space Industries\RSI Launcher\Uninstall RSI Launcher.exe" /allusers'
            string? uninstallString = (string?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{RSI_LAUNCHER_REGISTRY_KEY}", "UninstallString", null);
            LoggerFactory.LogInformation("uninstallString = '" + uninstallString + "'");

            if (uninstallString != null) {
                int firstQuotePos = uninstallString.IndexOf('"');
                int secondQuotePos = uninstallString.IndexOf('"', firstQuotePos+1);
                string? launcherUninstallerExecutablePath = null;

                try {
                    launcherUninstallerExecutablePath = uninstallString.Substring(firstQuotePos, secondQuotePos);
                } catch (ArgumentOutOfRangeException) {
                }

                if (launcherUninstallerExecutablePath != null) {
                    string? rsiLauncherFolderPath = Path.GetDirectoryName(displayIcon);

                    if (rsiLauncherFolderPath != null && IsValidFolderPath(rsiLauncherFolderPath)) {
                        return rsiLauncherFolderPath;
                    }
                }
            }

            return null;
        }

        #endregion
	}
}
