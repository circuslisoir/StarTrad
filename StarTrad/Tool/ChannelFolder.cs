using System.IO;
using System.Text;

namespace StarTrad.Tool
{
    /// <summary>
	/// Represents a channel directory where the game is installed, for exemple
    /// "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE".
	/// </summary>
	public class ChannelFolder
	{
		public const string GLOBAL_INI_FILE_NAME = "global.ini";
        public const string PREFERED_CHANNEL_NAME = "LIVE";

        private readonly LibraryFolder libraryFolder;
		private readonly string channelName;

        /*
		Constructor
		*/

		private ChannelFolder(LibraryFolder libraryFolder, string channelName)
        {
            this.libraryFolder = libraryFolder;
			this.channelName = channelName;
        }

        /*
		Static
		*/

		/// <summary>
		/// Makes an instance of this class for the first channel directory we can find.
		/// </summary>
		/// <returns></returns>
		public static ChannelFolder? MakeFirst(LibraryFolder libraryFolder, bool askForPathIfNeeded = false)
		{
			string? channelFolderPath = GetFirstExistingChannelFolderPath();

            if (channelFolderPath == null) {
                return null;
            }

            return ChannelFolder.MakeFromName(
                libraryFolder,
                System.IO.Path.GetFileName(channelFolderPath),
                askForPathIfNeeded
            );
		}

        /// <summary>
        /// Makes an instance of this class from the name of a channel.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ChannelFolder? MakeFromName(LibraryFolder libraryFolder, string channelFolderName, bool askForPathIfNeeded = false)
        {
            string? libraryFolderPath = LibraryFolder.GetFolderPath();

            if (askForPathIfNeeded && libraryFolderPath == null) {
                View.Window.Path pathWindow = new View.Window.Path();
                libraryFolderPath = pathWindow.LibraryFolderPath;
            }

			if (libraryFolderPath == null) {
				return null;
			}

            return MakeFromPath(
                libraryFolder,
                libraryFolder.BuildChannelFolderPath(channelFolderName)
            );
        }

        public static ChannelFolder? MakeFromPath(LibraryFolder libraryFolder, string channelFolderPath)
        {
            if (!Directory.Exists(channelFolderPath)) {
                return null;
            }

			return new ChannelFolder(libraryFolder, System.IO.Path.GetFileName(channelFolderPath));
        }

		/// <summary>
        /// Checks if the given path is a channel ("LIVE", "PTU", ...) directory where the game is installed.
        /// </summary>
        /// <param name="path">
        /// Example: "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE"
        /// </param>
        /// <returns></returns>
        public static bool IsValidChannelFolderPath(string path)
        {
            return Directory.Exists(path)
                && File.Exists(path + @"\Data.p4k");
        }

        /// <summary>
        /// Reads the installed translation file, if any, in order to obtain its version.
        /// </summary>
        /// <returns>
        /// The installed version as an object, or null if the file cannot be found.
        /// </returns>
        public TranslationVersion? GetInstalledTranslationVersion()
        {
            string? installedGlobalIniFilePath = this.GlobalIniInstallationFilePath;

            if (!File.Exists(installedGlobalIniFilePath)) {
                return null;
            }

            string versionCommentToken = "; Version :";

            using (FileStream fileStream = File.OpenRead(installedGlobalIniFilePath)) {
                using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128)) {
                    string? line;

                    while ((line = streamReader.ReadLine()) != null) {
                        if (!line.StartsWith(versionCommentToken)) {
                            continue;
                        }

                        TranslationVersion? version = TranslationVersion.Make(line.Replace(versionCommentToken, ""));

                        if (version == null) {
                            continue;
                        }

                        return version;
                    }
                }
            }

            return null;
        }

        /*
        Private
        */

        /// <summary>
        /// Gets the absolute path to the first valid channel directory we can find.
        /// Ideally this would be the one configured in settings, but it it doesn't exist we'll try to find another one.
        /// </summary>
        /// <returns>
        /// The absolute path to a directory, or null if it cannot be found.
        /// Example: "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE".
        /// </returns>
        private static string? GetFirstExistingChannelFolderPath()
        {
            // Check for the channel configured in settings
            string? pathFromSettings = GetChannelFolderPath(Properties.Settings.Default.RsiLauncherChannel);

            if (pathFromSettings != null) {
                return pathFromSettings;
            }

            string[] channelFolderPaths = LibraryFolder.ListAvailableChannelFolderPaths();

            if (channelFolderPaths.Length < 1) {
                return null;
            }

            return channelFolderPaths[0];
        }

        /// <summary>
        /// Obtains the path to the Star Citizen installation directory.
        /// </summary>
        /// <param name="channel">"LIVE", "PTU", ...</param>
        /// <returns>
        /// The absolute path to a directory, or null if it cannot be found.
        /// Example: "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE".
        /// </returns>
        private static string? GetChannelFolderPath(string channel)
        {
            Logger.LogInformation("Récupération du dossier d'installation SC");
            string? libraryFolderPath = LibraryFolder.GetFolderPath();

            if (libraryFolderPath == null)
            {
                return null;
            }

            string starCitizenDirectoryPath = libraryFolderPath + '\\' + LibraryFolder.STAR_CITIZEN_DIRECTORY_NAME + '\\' + channel;

            if (!IsValidChannelFolderPath(starCitizenDirectoryPath)) {
                return null;
            }

            return starCitizenDirectoryPath;
        }

		/*
		Accessor
		*/

        /// <summary>
        /// The name of the channel folder, for example "LIVE" or "PTU".
        /// </summary>
        public string Name
		{
			get { return System.IO.Path.GetFileName(this.Path); }
		}

        /// <summary>
        /// The absolute path to the Library Folder, by default:
        /// "C:\Program Files\Roberts Space Industries".
        /// </summary>
		public string Path
		{
			get { return this.libraryFolder.BuildChannelFolderPath(this.channelName); }
		}

        /// <summary>
        /// The absolute path to the user.cfg file, by default:
        /// "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\user.cfg".
        /// </summary>
		public string UserCfgFilePath
        {
            get { return this.Path + '\\' + "user.cfg"; }
        }

        /// <summary>
        /// Gets the absolute path to the directory where the global.ini file should be installed.
        /// For a default Star Citizen installation, this should be "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\data\Localization\french_(france)".
        /// </summary>
        public string GlobalIniInstallationDirectoryPath
        {
            get { return this.Path + @"\data\Localization\french_(france)"; }
        }

        /// <summary>
        /// Returns the absolute path to the final destination of the global.ini file.
        /// For a default Star Citizen installation, this should be "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\data\Localization\french_(france)\global.ini".
        /// </summary>
        public string GlobalIniInstallationFilePath
        {
            get { return this.GlobalIniInstallationDirectoryPath + '\\' + GLOBAL_INI_FILE_NAME; }
        }

        public bool IsLiveChannel
        {
            get { return this.channelName == "LIVE"; }
        }
	}
}
