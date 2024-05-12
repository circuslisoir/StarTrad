using System.IO;
using System.Collections.Generic;
using System.Text;
using System;
using StarTrad.Properties;

namespace StarTrad.Tool
{
	/// <summary>
	/// Represents the RSI Launcher's Library Folder, by default
	/// "C:\Program Files\Roberts Space Industries".
	/// </summary>
	public class LibraryFolder
	{
		public const string STAR_CITIZEN_DIRECTORY_NAME = "StarCitizen";
		public const string DEFAULT_LIBRARY_FOLDER_PATH = @"C:\Program Files\Roberts Space Industries";

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
			string? libraryFolderPath = LibraryFolder.GetFolderPath();

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
			return ListAvailableChannelDirectoriesAt(path).Length >= 1;
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
		public static string[] ListAvailableChannelFolderPaths()
		{
			string? libraryFolderPath = LibraryFolder.GetFolderPath();

			if (libraryFolderPath == null) {
				return [];
			}

			return ListAvailableChannelDirectoriesAt(libraryFolderPath);
		}

		/// <summary>
		/// Tries to list the channel folders located under the given library folder path.
		/// Also, the "LIVE" channel will always be the first item of the returned list, unless it's unavailable.
		/// </summary>
		/// <param name="libraryFolderPath"></param>
		/// <returns></returns>
		private static string[] ListAvailableChannelDirectoriesAt(string libraryFolderPath)
		{
			if (!Directory.Exists(libraryFolderPath)) {
				return [];
			}

			string startCitizenDirectoryPath = libraryFolderPath + '\\' + STAR_CITIZEN_DIRECTORY_NAME;

			if (!Directory.Exists(startCitizenDirectoryPath)) {
				return [];
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

			return channelDirectoryPaths.ToArray();
		}

		/// <summary>
		/// Obtains the path to the Star Citizen's Library Folder from the settings.
		/// If the stored path is no longer valid, tries to find it again.
		/// </summary>
		/// <returns>
		/// The absolute path to a directory, or null if it cannot be found.
		/// Example: "C:\Program Files\Roberts Space Industries".
		/// </returns>
		public static string? GetFolderPath()
		{
			string? libraryFolderPath = Settings.Default.RsiLauncherLibraryFolder;

			if (LibraryFolder.IsValidLibraryFolderPath(libraryFolderPath)) {
				return libraryFolderPath;
			}

			// The library folder path stored in settings is no longer valid, we'll try to find it again
			libraryFolderPath = FindLibraryFolderPath();

			// Keep the path in settings so we won't need to look lor it again next time
			Settings.Default.RsiLauncherLibraryFolder = (libraryFolderPath != null ? libraryFolderPath : "");
			Settings.Default.Save();

			return libraryFolderPath;
		}

		/// <summary>
		/// Yields a ChannelFolder object for each available channel folders.
		/// </summary>
		/// <param name="settingsOnly">
		/// If true, results will only include channels selected in the settings.
		/// </param>
		/// <returns></returns>
		public IEnumerable<ChannelFolder> EnumerateChannelFolders(bool settingsOnly = false, bool withInstalledTranslation = false)
		{
			string[] channelFolderPaths;

			if (settingsOnly && Settings.Default.RsiLauncherChannel != View.Window.Settings.CHANNEL_ALL) {
				channelFolderPaths = [this.BuildChannelFolderPath(Settings.Default.RsiLauncherChannel)];
			} else {
				channelFolderPaths = ListAvailableChannelFolderPaths();
			}

			foreach (string channelFolderPath in channelFolderPaths) {
				ChannelFolder? channelFolder = ChannelFolder.MakeFromPath(this, channelFolderPath);

				if (channelFolder == null) {
				   continue;
				}

				if (withInstalledTranslation) {
					TranslationVersion? translationVersion = channelFolder.GetInstalledTranslationVersion();
					if (translationVersion == null) continue;
				}

				 yield return channelFolder;
			}
		}

		public string BuildChannelFolderPath(string channelName)
		{
			return this.StarCitizenDirectoryPath + '\\' + channelName;
		}

		#region Library Folder path finding methods

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
		private static string? FindLibraryFolderPathFromRegistry()
		{
			string? rsiLauncherFolderPath = RsiLauncherFolder.GetFolderPath();

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
		/// Returns the path to the user directory, for exemple "C:\Users\<UserName>"
		/// </summary>
		private static string UserDirectory
		{
			get { return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); }
		}
	}
}
