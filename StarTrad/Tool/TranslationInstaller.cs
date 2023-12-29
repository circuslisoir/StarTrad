using System;
using System.Net;
using System.IO;
using StarTrad.Properties;
using System.Text;

namespace StarTrad.Tool
{
	/// <summary>
	/// Downloads then installs a Star Citizen translation.
	/// </summary>
	internal class TranslationInstaller
	{
		private const string GLOBAL_INI_FILE_NAME = "global.ini";

		/*
		Public
		*/

		/// <summary>
		/// Installs, if needed, the latest version of the translation from the circuspes website.
		/// </summary>
		public void InstallLatestTranslation()
		{
			TranslationVersion latestVersion = this.QueryLatestAvailableTranslationVersion();

			// Unable to obtain the remote version
			if (latestVersion == null) {
				return;
			}

			TranslationVersion installedVersion = this.GetInstalledTranslationVersion();

			// We already have the latest version installed
			if (installedVersion != null && latestVersion.IsNewerThan(installedVersion)) {
				return;
			}

			this.StartGlobalIniFileDownload();
		}

		/*
		Private
		*/

		/// <summary>
		/// Reads the installed translation file, if any, in order to obtain its version.
		/// </summary>
		/// <returns>
		/// The installed version as an object, or null if the file cannot be found.
		/// </returns>
		private TranslationVersion GetInstalledTranslationVersion()
		{
			string installedGlobalIniFilePath = this.GlobalIniInstallationFilePath;

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
		private TranslationVersion QueryLatestAvailableTranslationVersion()
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
			string localGlobalIniFilePath = App.workingDirectoryPath + '\\' + GLOBAL_INI_FILE_NAME;

			if (File.Exists(localGlobalIniFilePath)) {
				File.Delete(localGlobalIniFilePath);
			}

			WebClient client = new WebClient();

			client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(WebClient_GlobalIniFileDownloadProgress);
			client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(WebClient_GlobalIniFileDownloadCompleted);
			client.DownloadFileAsync(new Uri(CircuspesClient.HOST + "/download/" + GLOBAL_INI_FILE_NAME), localGlobalIniFilePath);

			client.Dispose();
		}

		/// <summary>
		/// Given the location of a local global.ini file, installs it in the correct directory.
		/// </summary>
		/// <param name="downloadedGlobalIniFilePath"></param>
		private void InstallGlobalIniFile(string downloadedGlobalIniFilePath)
		{
			string starCitizenDirectory = LibraryFolderFinder.GetStarCitizenInstallDirectoryPath(Settings.Default.RsiLauncherChannel);

			if (!Directory.Exists(starCitizenDirectory)) {
				return;
			}

			string globalIniDestinationDirectoryPath = this.GlobalIniInstallationDirectoryPath;

			// Create destination directory if need
			if (!Directory.Exists(globalIniDestinationDirectoryPath)) {
				try {
					Directory.CreateDirectory(globalIniDestinationDirectoryPath);
				} catch (Exception) {
					return;
				}
			}

			// Move downloaded file
			try {
				File.Move(downloadedGlobalIniFilePath, this.GlobalIniInstallationFilePath);
			} catch (Exception) {
				return;
			}

			// Create the user.cfg file
			try {
				File.WriteAllLines(starCitizenDirectory + @"\user.cfg", new string[] {
					"g_language = french_(france)"
				});
			} catch (Exception) {
				return;
			}
		}

		/*
		Accessor
		*/

		/// <summary>
		/// Gets the absolute path to the directory where the global.ini file should be installed.
		/// For a default Star Citizen installation, this should be "C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\data\Localization\french_(france)".
		/// </summary>
		private string GlobalIniInstallationDirectoryPath
		{
			get { return LibraryFolderFinder.GetStarCitizenInstallDirectoryPath(Settings.Default.RsiLauncherChannel) + @"\data\Localization\french_(france)"; }
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

			Console.WriteLine("Downloading " + GLOBAL_INI_FILE_NAME + ": " + percentage + " % (" + (e.BytesReceived / 1000000) + "Mo / " + (e.TotalBytesToReceive / 1000000) + "Mo)");
		}

		/// <summary>
		/// Called once the global.ini has been downloaded.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void WebClient_GlobalIniFileDownloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (e.Error != null) {
				return;
			}

			string localGlobalIniFilePath = App.workingDirectoryPath + '\\' + GLOBAL_INI_FILE_NAME;

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
