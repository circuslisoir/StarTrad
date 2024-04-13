using System.IO;
using System.Linq;
using System.Diagnostics;

namespace Updater
{
	class Program
	{
		const string TEMPDIR = @"temp\";
		const string APPEXE  = "StarTrad.exe";
		const string UPDATER = "Updater.exe";
		const string LOGDIR  = @"logs\";
		const string LOGFILE = "updater.log";

		private string extracted = null;

		static int Main(string[] args)
		{
			if (args.Count() == 0) {
				return 0;
			}

			Program updater = new Program();

			updater.extracted = args[0];

			updater.CheckStarTradExe();

			if (!updater.ReplaceFiles()) {
				return 1;
			}

			updater.LaunchApp();

			return 0;
		}

		/// <summary>
		/// Verify that StarTrad.exe isn't running.
		/// Kill it if needed.
		/// </summary>
		private void CheckStarTradExe()
		{
			Process[] processes = Process.GetProcessesByName(APPEXE);

			foreach (Process process in processes) {
				process.Kill();
				process.WaitForExit(1000 * 3); // 5 seconds timeout
				process.Dispose();
			}

			System.Threading.Thread.Sleep(1000);
		}

		/// <summary>
		/// Replace StarTrad files by the extracted files.
		/// </summary>
		private bool ReplaceFiles()
		{
			if (!Directory.Exists(this.Extracted)) {
				return false;
			}

			// Create all of the directories
			foreach (string dirPath in Directory.GetDirectories(this.Extracted, "*", SearchOption.AllDirectories)) {
				try {
					Directory.CreateDirectory(dirPath.Replace(this.Extracted + @"\", ""));
				} catch {
					this.AppendLog("Can't create direcotry: " + dirPath);
				}
			}

			// Copy all the files & Replaces any files with the same name
			foreach (string newPath in Directory.GetFiles(this.Extracted, "*.*", SearchOption.AllDirectories)) {
				// Don't replace the updater since it's running
				if (newPath == this.Extracted + @"\" + UPDATER) {
					continue;
				}

				try {
					File.Copy(newPath, newPath.Replace(this.Extracted + @"\", ""), true);
				} catch {
					this.AppendLog("Can't copy file: " + newPath);
				}
			}

			// Delete the extracted folder
			try {
				Directory.Delete(this.Extracted, true);
			} catch {
				this.AppendLog("Can't delete directory: " + this.Extracted);
			}

			return true;
		}

		/// <summary>
		/// Launch StarTrad again.
		/// </summary>
		private void LaunchApp()
		{
			if (!File.Exists(APPEXE)) {
				this.AppendLog("File " + APPEXE + " does not exists");
				return;
			}

			try {
				Process process = new Process();
				process.StartInfo.FileName = APPEXE;
				process.StartInfo.Arguments = "/updated";
				process.Start();
			} catch {
				this.AppendLog("Unable to start " + APPEXE);
				return;
			}
		}

		private void AppendLog(string line, string type="ERROR")
		{
			if (!Directory.Exists(LOGDIR)) {
				Directory.CreateDirectory(LOGDIR);
			}

			string filepath = LOGDIR + LOGFILE;
			StreamWriter sw = null;

			// This text is added only once to the file.
			if (!File.Exists(filepath)) {
				sw = File.CreateText(filepath);
			} else {
				sw = File.AppendText(filepath);
			}

			sw.WriteLine("[" + type + "] - " + line);

			sw.Close();
			sw.Dispose();
		}

		private string Extracted
		{
			get { return TEMPDIR + this.extracted; }
		}
	}
}
