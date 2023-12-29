using System.Net;
using System.IO;
using System.Text;
using System.Diagnostics;
using StarTrad.Properties;
using StarTrad.Helper;

namespace StarTrad.Tool
{
	/// <summary>
	/// Downloads then installs a Star Citizen translation.
	/// </summary>
	internal class TranslationInstaller
	{
		private const string GLOBAL_INI_FILE_NAME = "global.ini";

		// The absolute path to the Star Citizen's installation directory for the configured channel, for example:
		// "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE".
		private string currentChannelDirectoryPath;

		/*
		Constructor
		*/

		private TranslationInstaller(string currentChannelDirectoryPath)
		{
			this.currentChannelDirectoryPath = currentChannelDirectoryPath;
		}

		/*
		Static
		*/

		/// <summary>
		/// Runs the whole installation process, if possible.
		/// </summary>
		public static void Run()
		{
			string? currentChannelDirectoryPath = LibraryFolderFinder.GetStarCitizenInstallDirectoryPath(Settings.Default.RsiLauncherChannel);

			if (currentChannelDirectoryPath == null) {
				return;
			}

			TranslationInstaller installer = new TranslationInstaller(currentChannelDirectoryPath);
			installer.InstallLatestTranslation();
		}

		/*
		Private
		*/

		/// <summary>
		/// Installs, if needed, the latest version of the translation from the circuspes website.
		/// </summary>
		private void InstallLatestTranslation()
		{
			TranslationVersion? latestVersion = this.QueryLatestAvailableTranslationVersion();

			// Unable to obtain the remote version
			if (latestVersion == null) {
				return;
			}

			TranslationVersion? installedVersion = this.GetInstalledTranslationVersion();

			// We already have the latest version installed
			if (installedVersion != null && latestVersion.IsNewerThan(installedVersion)) {
				return;
			}

			this.StartGlobalIniFileDownload();
		}

		/// <summary>
		/// Reads the installed translation file, if any, in order to obtain its version.
		/// </summary>
		/// <returns>
		/// The installed version as an object, or null if the file cannot be found.
		/// </returns>
		private TranslationVersion? GetInstalledTranslationVersion()
		{
			string? installedGlobalIniFilePath = this.GlobalIniInstallationFilePath;

			if (!File.Exists(installedGlobalIniFilePath)) {
				return null;
			}

			string versionCommentToken = "; Version :";

			using (FileStream fileStream = File.OpenRead(installedGlobalIniFilePath)) {
				using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128)) {
					String line;

					while ((line = streamReader.ReadLine()) != null) {
						if (line.StartsWith(versionCommentToken)) {
							return TranslationVersion.Make(line.Replace(versionCommentToken, ""));
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Obtains an object representing the latest version of the translation.
		/// </summary>
		/// <returns></returns>
		private TranslationVersion? QueryLatestAvailableTranslationVersion()
		{
			string html = CircuspesClient.GetRequest("/download/version.html");

			if (String.IsNullOrWhiteSpace(html)) {
				return null;
			}

			return TranslationVersion.Make(html);
		}

		/// <summary>
		/// Starts downloading the global.ini translation file from the circuspes website.
		/// </summary>
		private void StartGlobalIniFileDownload()
		{
			// Define where to store the to-be-downloaded file
			string localGlobalIniFilePath = App.workingDirectoryPath + '\\' + GLOBAL_INI_FILE_NAME;

			if (File.Exists(localGlobalIniFilePath)) {
				File.Delete(localGlobalIniFilePath);
			}

			WebClient client = new WebClient();

			client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(this.WebClient_GlobalIniFileDownloadProgress);
			client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(this.WebClient_GlobalIniFileDownloadCompleted);
			client.DownloadFileAsync(new Uri(CircuspesClient.HOST + "/download/" + GLOBAL_INI_FILE_NAME), localGlobalIniFilePath);

			client.Dispose();
		}

		/// <summary>
		/// Given the location of a local global.ini file, installs it in the correct directory.
		/// </summary>
		/// <param name="downloadedGlobalIniFilePath"></param>
		private void InstallGlobalIniFile(string downloadedGlobalIniFilePath)
		{
			string globalIniDestinationDirectoryPath = this.GlobalIniInstallationDirectoryPath;

			try {
				// Create destination directory if need
				if (!Directory.Exists(globalIniDestinationDirectoryPath)) {
					Directory.CreateDirectory(globalIniDestinationDirectoryPath);
				}

				// Move downloaded file
				File.Move(downloadedGlobalIniFilePath, this.GlobalIniInstallationFilePath, true);

				// Create the user.cfg file
				File.WriteAllLines(this.UserCfgFilePath, new string[] {
					"g_language = french_(france)"
				});
			} catch (Exception e) {
				LoggerFactory.LogError(e);
			}
		}

		/*
		Accessor
		*/

		/// <summary>
		/// Returns the absolute path to the final destination of the global.ini file.
		/// For a default Star Citizen installation, this should be "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\data\Localization\french_(france)\global.ini".
		/// </summary>
		private string UserCfgFilePath
		{
			get { return this.currentChannelDirectoryPath + '\\' + "user.cfg"; }
		}

		/// <summary>
		/// Gets the absolute path to the directory where the global.ini file should be installed.
		/// For a default Star Citizen installation, this should be "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\data\Localization\french_(france)".
		/// </summary>
		private string GlobalIniInstallationDirectoryPath
		{
			get { return this.currentChannelDirectoryPath + @"\data\Localization\french_(france)"; }
		}

		/// <summary>
		/// Returns the absolute path to the final destination of the global.ini file.
		/// For a default Star Citizen installation, this should be "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\data\Localization\french_(france)\global.ini".
		/// </summary>
		private string GlobalIniInstallationFilePath
		{
			get { return this.GlobalIniInstallationDirectoryPath + '\\' + GLOBAL_INI_FILE_NAME; }
		}

		/*
		Event
		*/

		/// <summary>
		/// Called periodically as the download of the global.ini file progresses.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void WebClient_GlobalIniFileDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
		{
			int percentage = (int)(((float)e.BytesReceived / (float)e.TotalBytesToReceive) * 100.0);

			Debug.WriteLine("Downloading " + GLOBAL_INI_FILE_NAME + ": " + percentage + " % (" + (e.BytesReceived / 1000000) + "Mo / " + (e.TotalBytesToReceive / 1000000) + "Mo)");
		}

		/// <summary>
		/// Called once the global.ini has been downloaded.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void WebClient_GlobalIniFileDownloadCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (e.Error != null) {
				return;
			}

			string localGlobalIniFilePath = App.workingDirectoryPath + GLOBAL_INI_FILE_NAME;

			if (!File.Exists(localGlobalIniFilePath)) {
				return;
			}

			long length = new FileInfo(localGlobalIniFilePath).Length;

			// File is empty, download failed
			if (length <= 0) {
				return;
			}

			this.InstallGlobalIniFile(localGlobalIniFilePath);
		}
	}
}
