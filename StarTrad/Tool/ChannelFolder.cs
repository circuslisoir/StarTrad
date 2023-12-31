﻿using System.IO;
using System.Collections.Generic;
using StarTrad.Helper;

namespace StarTrad.Tool
{
    /// <summary>
	/// Represents a channel directory where the game is installed, for exemple
    /// "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE".
	/// </summary>
	internal class ChannelFolder : LibraryFolder 
	{
		public const string GLOBAL_INI_FILE_NAME = "global.ini";
        public const string PREFERED_CHANNEL_NAME = "LIVE";

		private readonly string channelName;

        /*
		Constructor
		*/

		private ChannelFolder(string libraryFolderPath, string channelName) : base(libraryFolderPath)
        {
			this.channelName = channelName;
        }

        /*
		Static
		*/

		/// <summary>
		/// Makes an instance of this class but only if the can find the channel directory where the game is installed.
		/// </summary>
		/// <returns></returns>
		new public static ChannelFolder? Make(bool askForPathIfNeeded = false)
		{
			string? libraryFolderPath = LibraryFolder.GetFolderPath();

            if (askForPathIfNeeded && libraryFolderPath == null) {
                View.Window.Path pathWindow = new View.Window.Path();
                libraryFolderPath = pathWindow.LibraryFolderPath;
            }

			if (libraryFolderPath == null) {
				return null;
			}

			string? channelFolderPath = GetFirstExistingChannelFolderPath();

            if (channelFolderPath == null) {
                return null;
            }

			return new ChannelFolder(libraryFolderPath, System.IO.Path.GetFileName(channelFolderPath));
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

            List<string> channelFolderPaths = ListAvailableChannelDirectories();

            if (channelFolderPaths.Count < 1) {
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
            LoggerFactory.LogInformation("Récupération du dossier d'installation SC");
            string? libraryFolderPath = LibraryFolder.GetFolderPath();

            if (libraryFolderPath == null)
            {
                return null;
            }

            string starCitizenDirectoryPath = libraryFolderPath + '\\' + STAR_CITIZEN_DIRECTORY_NAME + '\\' + channel;

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
			get { return this.StarCitizenDirectoryPath + '\\' + this.channelName; }
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
	}
}
