using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

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
		public static LibraryFolder? Make(bool askForPathIfNeeded = false)
		{
			string? libraryFolderPath = LibraryFolderFinder.GetFromSettingsOrFindExisting();

            if (askForPathIfNeeded && libraryFolderPath == null) {
                View.Window.Path pathWindow = new View.Window.Path();
                libraryFolderPath = pathWindow.LibraryFolderPath;
            }

			if (libraryFolderPath == null) {
				return null;
			}

			return new LibraryFolder(libraryFolderPath);
		}

        /// <summary>
        /// Checks if the given path is a valid Library Folder path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsValidLibraryFolderPath(string path)
        {
            return ListAvailableChannelDirectoriesAt(path).Count >= 1;
        }

		/// <summary>
        /// Lists all the existing channel directories inside the StarCitizen directory.
        /// Also, the "LIVE" channel will always be the first item of the returned list, unless it's unavailable.
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
            string? libraryFolderPath = LibraryFolderFinder.GetFromSettingsOrFindExisting();

            if (libraryFolderPath == null) {
                return new List<string>();
            }

            return ListAvailableChannelDirectoriesAt(libraryFolderPath);
        }

        /// <summary>
        /// Tries to list the channel folders located under the given library folder path.
        /// Also, the "LIVE" channel will always be the first item of the returned list, unless it's unavailable.
        /// </summary>
        /// <param name="libraryFolderPath"></param>
        /// <returns></returns>
        private static List<string> ListAvailableChannelDirectoriesAt(string libraryFolderPath)
        {
            if (!Directory.Exists(libraryFolderPath)) {
                return new List<string>();
            }
            
            string startCitizenDirectoryPath = libraryFolderPath + '\\' + STAR_CITIZEN_DIRECTORY_NAME;

            if (!Directory.Exists(startCitizenDirectoryPath)) {
                return new List<string>();
            }

            List<string> channelDirectoryPaths = new List<string>();

            foreach (string directoryPath in Directory.GetDirectories(startCitizenDirectoryPath)) {
                if (ChannelFolder.IsValidChannelFolderPath(directoryPath)) {
                   channelDirectoryPaths.Add(directoryPath);
                }
            }

            channelDirectoryPaths.Sort(delegate(string a, string b) {
                // Always put the LIVE channel at the top of the list
                if (Path.GetFileName(a) == ChannelFolder.PREFERED_CHANNEL_NAME) {
                    return -1;
                }

                // Every other channels will be put at the bottom
                return 1;
            });

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
