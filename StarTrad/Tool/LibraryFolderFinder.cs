using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using StarTrad.Helper;

namespace StarTrad.Tool
{
    /// <summary>
    /// Various methods for finding the Star Citizen installation path.
    /// </summary>
    internal class LibraryFolderFinder
    {
        public const string DEFAULT_LIBRARY_FOLDER_PATH = @"C:\Program Files\Roberts Space Industries";
        public const string RSI_LAUNCHER_EXECUTABLE_FILE_NAME = "RSI Launcher.exe";
        private const string RSI_LAUNCHER_REGISTRY_KEY = "81bfc699-f883-50c7-b674-2483b6baae23";

        /*
		Public
		*/

        /// <summary>
        /// Obtains the path to the Star Citizen's Library Folder from the settings.
        /// If the stored path is no longer valid, tries to find it again.
        /// </summary>
        /// <returns>
        /// The absolute path to a directory, or null if it cannot be found.
        /// Example: "C:\Program Files\Roberts Space Industries".
        /// </returns>
        public static string? GetFromSettingsOrFindExisting()
        {
            string? libraryFolderPath = Properties.Settings.Default.RsiLauncherLibraryFolder;

            if (LibraryFolder.IsValidLibraryFolderPath(libraryFolderPath)) {
                return libraryFolderPath;
            }

            // The library folder path stored in settings is no longer valid, we'll try to find it again
            libraryFolderPath = FindLibraryFolderPath();

            // Keep the path in settings so we won't need to look lor it again next time
            Properties.Settings.Default.RsiLauncherLibraryFolder = (libraryFolderPath != null ? libraryFolderPath : "");
            Properties.Settings.Default.Save();

            return libraryFolderPath;
        }

        /*
		Private
		*/

        /// <summary>
        /// Tries to find the path to the Star Citizen's Library Folder using various methods.
        /// </summary>
        /// <returns>
        /// The absolute path to a directory, or null if it cannot be found.
        /// Example: "C:\Program Files\Roberts Space Industries".
        /// </returns>
        private static string? FindLibraryFolderPath()
        {
            if (LibraryFolder.IsValidLibraryFolderPath(DEFAULT_LIBRARY_FOLDER_PATH)) {
                return DEFAULT_LIBRARY_FOLDER_PATH;
            }

            string? libraryfolderPath = FindLibraryFolderPathFromRegistry();

            if (libraryfolderPath != null) {
                return libraryfolderPath;
            }

            libraryfolderPath = FindLibraryFolderPathFromLauncherLocalStorage();

            if (libraryfolderPath != null) {
                return libraryfolderPath;
            }

            libraryfolderPath = FindLibraryFolderPathFromLauncherLogFile();

            if (libraryfolderPath != null) {
                return libraryfolderPath;
            }

            return null;
        }

		#region Library Folder finding methods

		/// <summary>
		/// Attemps to find the library folder by reading the RSI Launcher's Local Storage database.
		/// </summary>
		/// <returns>
		/// The absolute path to a directory, or null if it cannot be found.
		/// Example: "C:\Program Files\Roberts Space Industries".
		/// </returns>
		private static string? FindLibraryFolderPathFromLauncherLocalStorage()
        {
            string leveldbDirectoryPath = UserDirectory + @"\AppData\Roaming\rsilauncher\Local Storage\leveldb";

            if (!Directory.Exists(leveldbDirectoryPath)) {
                return null;
            }

            // Open a connection to a new DB and create if not found
            LevelDB.Options options = new LevelDB.Options { CreateIfMissing = false };
            LevelDB.DB db;

            // Accessing the local storage might fail if the launcher is running
            try {
                db = new LevelDB.DB(options, leveldbDirectoryPath);
            } catch (UnauthorizedAccessException) {
                return null;
            }

            string libraryFolderPath = db.Get("library-folder");

            db.Close();

            if (String.IsNullOrWhiteSpace(libraryFolderPath)) {
                return null;
            }

            if (!LibraryFolder.IsValidLibraryFolderPath(libraryFolderPath)) {
                return null;
            }

            return libraryFolderPath;
        }

        /// <summary>
        /// Attemps to find where the launcher is located then using that to find where the Library Folder is.
        /// </summary>
        /// <returns></returns>
        public static string? FindLibraryFolderPathFromRegistry()
        {
            string? rsiLauncherFolderPath = FindRsiLauncherFolderFromRegistry();

            if (rsiLauncherFolderPath == null) {
                return null;
            }

            string? libraryFolderPath = Path.GetDirectoryName(rsiLauncherFolderPath);

            if (libraryFolderPath == null || !LibraryFolder.IsValidLibraryFolderPath(libraryFolderPath)) {
                return null;
            }

            return libraryFolderPath;
        }

        /// <summary>
        /// Attemps to find the library folder by reading the RSI Launcher's log file.
        /// </summary>
        /// <returns>
        /// The absolute path to a directory, or null if it cannot be found.
        /// Example: "C:\Program Files\Roberts Space Industries".
        /// </returns>
        private static string? FindLibraryFolderPathFromLauncherLogFile()
        {
            string logFilePath = UserDirectory + @"\AppData\Roaming\rsilauncher\logs\log.log";

            if (!File.Exists(logFilePath)) {
                return null;
            }

            uint lineNumber = 0;
            uint changeEventLineNumber = 0;
            string? libraryFolderLine = null;

            using (FileStream fileStream = File.OpenRead(logFilePath)) {
                using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128)) {
                    string? line;

                    while ((line = streamReader.ReadLine()) != null) {
                        lineNumber++;

                        if (line.Contains("CHANGE_LIBRARY_FOLDER")) {
                            changeEventLineNumber = lineNumber;

                            continue;
                        }

                        if (changeEventLineNumber > 0 && lineNumber == (changeEventLineNumber + 3)) {
                            libraryFolderLine = line;

                            continue;
                        }
                    }
                }
            }

            if (libraryFolderLine == null) {
                return null;
            }

            libraryFolderLine = libraryFolderLine.Trim("\" ".ToCharArray());

            if (!Directory.Exists(libraryFolderLine)) {
                return null;
            }

            return libraryFolderLine.Replace(@"\\", @"\");
        }

        #endregion

        #region RSI Launcher folder finding methods

        /// <summary>
        /// Attemps to find where the RSI Launcher is installed by reading the registry.
        /// </summary>
        /// <returns>
        /// The absolute path to the RSI Launcher folder, or null if it cannot be found.
        /// </returns>
        public static string? FindRsiLauncherFolderFromRegistry()
        {
            // 'C:\Program Files\Roberts Space Industries\RSI Launcher'
            string? installLocation = (string?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\{RSI_LAUNCHER_REGISTRY_KEY}", "InstallLocation", null);
            LoggerFactory.LogInformation("installLocation = '" + installLocation + "'");

            if (installLocation != null && IsValidRsiLauncherFolderPath(installLocation)) {
                 return installLocation;
            }

            // 'C:\Program Files\Roberts Space Industries\RSI Launcher\uninstallerIcon.ico'
            string? displayIcon = (string?)Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{RSI_LAUNCHER_REGISTRY_KEY}", "DisplayIcon", null);
            LoggerFactory.LogInformation("displayIcon = '" + displayIcon + "'");

            if (displayIcon != null && File.Exists(displayIcon)) {
                string? rsiLauncherFolderPath = Path.GetDirectoryName(displayIcon);

                if (rsiLauncherFolderPath != null && IsValidRsiLauncherFolderPath(rsiLauncherFolderPath)) {
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

                    if (rsiLauncherFolderPath != null && IsValidRsiLauncherFolderPath(rsiLauncherFolderPath)) {
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

                    if (rsiLauncherFolderPath != null && IsValidRsiLauncherFolderPath(rsiLauncherFolderPath)) {
                        return rsiLauncherFolderPath;
                    }
                }
            }

            return null;
        }

        #endregion

        /// <summary>
        /// Checks if the given path is a directory containing the RSI Launcher.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool IsValidRsiLauncherFolderPath(string path)
        {
            return Directory.Exists(path)
                && File.Exists(path + '\\' + RSI_LAUNCHER_EXECUTABLE_FILE_NAME);
        }

        /*
		Accessor
		*/

        /// <summary>
        /// Returns the path to the user directory, for exemple "C:\Users\<UserName>"
        /// </summary>
        private static string UserDirectory
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); }
        }
    }
}
