using System.Diagnostics;
using System.IO;

namespace StarTrad.Tool
{
	/// <summary>
	/// Represents the RSI Launcher's Library Folder, by default
    /// "C:\Program Files\Roberts Space Industries".
	/// </summary>
	internal class LibraryFolder
	{
        protected const string STAR_CITIZEN_DIRECTORY_NAME = "StarCitizen";

		protected readonly string libraryFolderPath;

        /*
		Constructor
		*/

		protected LibraryFolder(string libraryFolderPath)
        {
            this.libraryFolderPath = libraryFolderPath;
        }

		/*
		Static
		*/

		/// <summary>
		/// Makes an instance of this class but only if we can actually find the Library Folder on the disk.
		/// </summary>
		/// <returns></returns>
		public static LibraryFolder? Make()
		{
			string? libraryFolderPath = LibraryFolderFinder.GetFromSettingsOrFindExisting();

			if (libraryFolderPath == null) {
				return null;
			}

			return new LibraryFolder(libraryFolderPath);
		}

		/// <summary>
        /// Lists all the existing channel directories inside the StarCitizen directory.
        /// </summary>
        /// <returns>
        /// Example: [
        ///     "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE",
        ///     "C:\Program Files\Roberts Space Industries\StarCitizen\PTU",
        ///     ...
        /// ]
        /// </returns>
        public static List<string> ListAvailableChannelDirectories()
        {
            List<string> channelDirectoryPaths = new List<string>();
            string? libraryFolderPath = LibraryFolderFinder.GetFromSettingsOrFindExisting();

            if (libraryFolderPath == null) {
                return channelDirectoryPaths;
            }
            
            string startCitizenDirectoryPath = libraryFolderPath + '\\' + STAR_CITIZEN_DIRECTORY_NAME;

            if (!Directory.Exists(startCitizenDirectoryPath)) {
                return channelDirectoryPaths;
            }

            foreach (string directoryPath in Directory.GetDirectories(startCitizenDirectoryPath)) {
                if (ChannelFolder.IsValidChannelFolderPath(directoryPath)) {
                   channelDirectoryPaths.Add(directoryPath);
                }
            }

            return channelDirectoryPaths;
        }

        /*
		Public
		*/

        public void ExecuteRsiLauncher()
		{
            string exePath = this.RsiLauncherExecutablePath;

            if (!File.Exists(exePath)) {
				return;
			}

            Process.Start(exePath);
        }

        /*
		Accessor
		*/

        /// <summary>
        /// The absolute path to the "StarCitizen" directory, by default:
        /// "C:\Program Files\Roberts Space Industries\StarCitizen".
        /// </summary>
        public string StarCitizenDirectoryPath
		{
			get { return this.libraryFolderPath + '\\' + STAR_CITIZEN_DIRECTORY_NAME; }
		}

        /// <summary>
        /// The absolute path to the RSI Launcher executable, by default:
        /// "C:\Program Files\Roberts Space Industries\RSI Launcher\RSI Launcher.exe".
        /// </summary>
        public string RsiLauncherExecutablePath
		{
			get { return this.libraryFolderPath + @"\RSI Launcher\RSI Launcher.exe"; }
		}
	}
}
