using System;
using System.IO;
using System.Text;

namespace StarTrad.View.Tool
{
	/// <summary>
	/// Various methods for finding the Star Citizen installation path.
	/// </summary>
	internal class LibraryFolderFinder
	{
		/*
		Public
		*/

		/// <summary>
		/// Obtains the path to the Star Citizen installation directory.
		/// </summary>
		/// <param name="channel">"LIVE", "PTU", ...</param>
		/// <returns>
		/// The absolute path to a directory, or null if it cannot be found.
		/// </returns>
		public static string GetStarCitizenInstallDirectoryPath(string channel)
		{
			string libraryFolderPath = FindLibraryFolderPath();

			if (libraryFolderPath == null) {
				return null;
			}

			string starCitizenDirectoryPath = libraryFolderPath + @"\StarCitizen\" + channel;

			if (!Directory.Exists(starCitizenDirectoryPath)) {
				return null;
			}

			// The directory does exists, we'll just check that the game is actuallty installed in it
			if (!File.Exists(starCitizenDirectoryPath + @"\Data.p4k")) {
				return null;
			}

			return starCitizenDirectoryPath;
		}

		/// <summary>
		/// Obtains the path to the Star Citizen's Library Folder from the settings.
		/// If the stored path is no longer valid, tries to find it again.
		/// </summary>
		/// <returns>
		/// The absolute path to a directory, or null if it cannot be found.
		/// </returns>
		public static string GetRsiLauncherLibraryFolderPath()
		{
			string libraryFolderPath = Properties.Settings.Default.RsiLauncherLibraryFolder;

			if (!String.IsNullOrWhiteSpace(libraryFolderPath) && Directory.Exists(libraryFolderPath)) {
				return libraryFolderPath;
			}

			// The library folder path stored in settings is no longer valid, we'll try to find it again
			libraryFolderPath = FindLibraryFolderPath();

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
		/// </returns>
		private static string FindLibraryFolderPath()
		{
			string libraryfolderPath = FindFromLauncherLocalStorage();

			if (!String.IsNullOrEmpty(libraryfolderPath)) {
				return libraryfolderPath;
			}

			libraryfolderPath = FindFromLauncherLogFile();

			if (!String.IsNullOrEmpty(libraryfolderPath)) {
				return libraryfolderPath;
			}

			return null;
		}

		/// <summary>
		/// Attemps to find the library folder by reading the RSI Launcher's Local Storage database.
		/// </summary>
		/// <returns>
		/// The absolute path to a directory, or null if it cannot be found.
		/// </returns>
		private static string FindFromLauncherLocalStorage()
		{
			string leveldbDirectoryPath = UserDirectory + @"\AppData\Roaming\rsilauncher\Local Storage\leveldb";

			if (!Directory.Exists(leveldbDirectoryPath)) {
				return null;
			}

			// Open a connection to a new DB and create if not found
			LevelDB.Options options = new LevelDB.Options { CreateIfMissing = false };
			LevelDB.DB db = new LevelDB.DB(options, leveldbDirectoryPath);

			string value = db.Get("library-folder");

			db.Close();

			if (String.IsNullOrWhiteSpace(value) || !Directory.Exists(value)) {
				return null;
			}

			return value;
		}

		/// <summary>
		/// Attemps to find the library folder by reading the RSI Launcher's log file.
		/// </summary>
		/// <returns>
		/// The absolute path to a directory, or null if it cannot be found.
		/// </returns>
		private static string FindFromLauncherLogFile()
		{
			string logFilePath = UserDirectory + @"\AppData\Roaming\rsilauncher\logs\log.log";

			if (!File.Exists(logFilePath)) {
				return null;
			}

			uint lineNumber = 0;
			uint changeEventLineNumber = 0;
			string libraryFolderLine = null;

			using (FileStream fileStream = File.OpenRead(logFilePath)) {
				using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128)) {
					String line;

					while ((line = streamReader.ReadLine()) != null)
					{
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

			return libraryFolderLine;
		}

		/*
		Getter
		*/

		private static string UserDirectory
		{
			get { return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); }
		}
	}
}
