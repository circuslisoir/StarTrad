using System.IO;
using System.Text;

namespace StarTrad.Tool
{
    /// <summary>
    /// Various methods for finding the Star Citizen installation path.
    /// </summary>
    internal class LibraryFolderFinder
    {
        private const string DEFAULT_LIBRARY_FOLDER_PATH = @"C:\Program Files\Roberts Space Industries";

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

            if (!string.IsNullOrWhiteSpace(libraryFolderPath) && Directory.Exists(libraryFolderPath))
            {
                return libraryFolderPath;
            }

            // The library folder path stored in settings is no longer valid, we'll try to find it again
            libraryFolderPath = Find();

            // Keep the path in settings so we won't need to look lor it again next time
            Properties.Settings.Default.RsiLauncherLibraryFolder = libraryFolderPath;
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
        private static string? Find()
        {
            string? libraryfolderPath = FindFromLauncherLocalStorage();

            if (!string.IsNullOrEmpty(libraryfolderPath))
            {
                return libraryfolderPath;
            }

            libraryfolderPath = FindFromLauncherLogFile();

            if (!string.IsNullOrEmpty(libraryfolderPath))
            {
                return libraryfolderPath;
            }

            return null;
        }

        /// <summary>
        /// Attemps to find the library folder by reading the RSI Launcher's Local Storage database.
        /// </summary>
        /// <returns>
        /// The absolute path to a directory, or null if it cannot be found.
        /// Example: "C:\Program Files\Roberts Space Industries".
        /// </returns>
        private static string? FindFromLauncherLocalStorage()
        {
            string leveldbDirectoryPath = UserDirectory + @"\AppData\Roaming\rsilauncher\Local Storage\leveldb";

            if (!Directory.Exists(leveldbDirectoryPath))
            {
                return null;
            }

            // Open a connection to a new DB and create if not found
            LevelDB.Options options = new LevelDB.Options { CreateIfMissing = false };
            LevelDB.DB db;

            // Accessing the local storage might fail if the launcher is running
            try
            {
                db = new LevelDB.DB(options, leveldbDirectoryPath);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            string libraryFolderPath = db.Get("library-folder");

            db.Close();

            // An empty value means that the default directory is being used
            if (string.IsNullOrWhiteSpace(libraryFolderPath))
            {
                libraryFolderPath = DEFAULT_LIBRARY_FOLDER_PATH;
            }

            if (!Directory.Exists(libraryFolderPath))
            {
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
        private static string? FindFromLauncherLogFile()
        {
            string logFilePath = UserDirectory + @"\AppData\Roaming\rsilauncher\logs\log.log";

            if (!File.Exists(logFilePath))
            {
                return null;
            }

            uint lineNumber = 0;
            uint changeEventLineNumber = 0;
            string? libraryFolderLine = null;

            using (FileStream fileStream = File.OpenRead(logFilePath))
            {
                using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128))
                {
                    string? line;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        lineNumber++;

                        if (line.Contains("CHANGE_LIBRARY_FOLDER"))
                        {
                            changeEventLineNumber = lineNumber;
                            continue;
                        }

                        if (changeEventLineNumber > 0 && lineNumber == (changeEventLineNumber + 3))
                        {
                            libraryFolderLine = line;
                            continue;
                        }
                    }
                }
            }

            if (libraryFolderLine == null)
            {
                return null;
            }

            libraryFolderLine = libraryFolderLine.Trim("\" ".ToCharArray());

            if (!Directory.Exists(libraryFolderLine))
            {
                return null;
            }

            return libraryFolderLine.Replace(@"\\", @"\");
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
