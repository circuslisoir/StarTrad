using System;
using System.IO;
using System.Windows;
using System.Threading;
using System.Diagnostics;

namespace StarTrad.View.Window
{
	/// <summary>
	/// Interaction logic for Updater.xaml
	/// </summary>
	public partial class Updater : System.Windows.Window
	{
		const string TEMPDIR   = @"temp\";
		const string ARCHIVE   = "extract.zip";
		const string EXTRACTED = "extracted";
		const string UPDATER   = "Updater.exe";
		const string GITUHB_URL = "https://github.com/circuslisoir/StarTrad";

		private string release;

		public Updater(string release)
		{
			InitializeComponent();

			this.release = release;
			this.Title = App.PROGRAM_NAME + " " + release;
			this.button_Cancel.IsEnabled = this.button_Update.IsEnabled = true;
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Called by the "Update" confirm button, start downloading.
		/// </summary>
		private void StartDownload(string url, string path)
		{
			Thread thread = new Thread(() => {
				System.Net.WebClient client = new System.Net.WebClient();

				client.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(Client_DownloadProgressChanged);
				client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(Client_DownloadFileCompleted);
				client.DownloadFileAsync(new Uri(url), path);

				client.Dispose();
			});

			thread.Start();
		}

		/// <summary>
		/// Install the downloaded version using the installer or the updater.
		/// </summary>
		private void InstallUpdate()
		{
			bool success = true;

			// Extract the zip
			if (!this.Unzip()) {
				this.Cancel(true);

				return;
			}

			// Replace the updater
			string newUpdater = this.Extracted + @"\" + UPDATER;

			if (File.Exists(newUpdater)) {
				File.Copy(newUpdater, UPDATER, true);
			}

			// Call the updater
			success = this.StartProcess(UPDATER, EXTRACTED);

			// Executable not found
			if (!success) {
				this.Cancel(true);

				return;
			}

			this.QuitProgram();
		}

		/// <summary>
		/// Unzip the archive.
		/// </summary>
		/// <returns></returns>
		private bool Unzip()
		{
			if (!File.Exists(this.Archive)) {
				return false;
			}

			// Folder already exists, remove everything in it
			if (Directory.Exists(this.Extracted)) {
				DirectoryInfo di = new DirectoryInfo(this.Extracted);

				foreach (FileInfo file in di.GetFiles()) {
					file.Delete();
				}

				foreach (DirectoryInfo dir in di.GetDirectories()) {
					dir.Delete(true);
				}
			}

			System.IO.Compression.ZipFile.ExtractToDirectory(this.Archive, this.Extracted);

			// Delete the archive
			try {
				File.Delete(this.Archive);
			} catch { }

			return true;
		}

		/// <summary>
		/// Start a process, returns true if success and false otherwise.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private bool StartProcess(string path, string args="")
		{
			if (!File.Exists(path)) {
				return false;
			}

			try {
				Process.Start(path, args);
			} catch {
				return false;
			}

			return true;
		}

		/// <summary>
		/// Cancel the update.
		/// Show a message if given then close the window.
		/// </summary>
		/// <param name="error"></param>
		private void Cancel(bool error=false)
		{
			if (error) {
				MessageBox.Show("Une erreur est survenue durant la mise à jour");
			}

			this.Close();
		}

		/// <summary>
		/// Terminate the current program instance.
		/// </summary>
		private void QuitProgram()
		{
			Application.Current.Shutdown();

			// Kill the process and wait for it to exit with a 3 seconds timeout
			Process process = Process.GetCurrentProcess();
			process.Kill();
			process.WaitForExit(1000 * 3);
			process.Dispose();
		}

		#endregion Private


		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string Changelog
		{
			set
			{
				this.WebBrowser_Changelog.NavigateToString("" +
				"<html lang=\"fr\">" +
					"<head>" +
						"<meta charset=\"UTF-8\">" +
						"<style>" +
							"ul { margin-left: 1em; }" +
						"</style>" +
					"</head>" +
					"<body style=\"background: #42525E; color: #CFDEE8\">" +
						value + "" +
					"</body>" +
				"</html>");
			}
		}

		private string Archive
		{
			get { return TEMPDIR + ARCHIVE; }
		}

		private string Extracted
		{
			get { return TEMPDIR + EXTRACTED; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Update the progress bar while downloading.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Client_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
		{
			Application.Current.Dispatcher.BeginInvoke((Action)(() => {
				double bytesIn = double.Parse(e.BytesReceived.ToString());
				double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
				double percentage = bytesIn / totalBytes * 100;

				this.downloadStatus.Content = (e.BytesReceived/1000) + "KB / " + (e.TotalBytesToReceive/1000) + "KB";
				this.downloadProgress.Value = int.Parse(Math.Truncate(percentage).ToString());
			}));
		}

		/// <summary>
		/// Download complete, close the window, call the updater then terminate the program.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			Application.Current.Dispatcher.BeginInvoke((Action)(() => {
				this.downloadProgress.Value = 100;
				this.InstallUpdate();
			}));
		}

		/// <summary>
		/// Initiate the update process when clicking on the Update button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Update_Click(object sender, RoutedEventArgs e)
		{
			this.button_Update.IsEnabled = false;
			this.downloadStatus.Content = "Démarrage...";

			if (!Directory.Exists(TEMPDIR)) {
				Directory.CreateDirectory(TEMPDIR);
			}

			string url = GITUHB_URL + "/releases/download/" + this.release + "/StarTrad.zip";

			this.StartDownload(url, this.Archive);
		}

		/// <summary>
		/// Just close the window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.Cancel();
		}

		#endregion Event
	}
}
