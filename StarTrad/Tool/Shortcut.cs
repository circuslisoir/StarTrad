using System;
using System.IO;
using System.Collections.Generic;
using StarTrad.Enum;
using IWshRuntimeLibrary;
using Shell32;
using System.Diagnostics;

namespace StarTrad.Tool
{
	/// <summary>
	/// Creates or reads shortcut files.
	/// </summary>
	internal class Shortcut
	{
		private const string EXTENSION = ".lnk";

		public readonly List<string> arguments = new();
		public string lnkFileName = App.PROGRAM_NAME;
		private string? iconFileName = null;

		/*
		Static
		*/

		public static string DesktopDirectoryPath
		{
			get { return Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
		}

		 public static string StartupShortcutPath
		{
			get { return Environment.GetFolderPath(Environment.SpecialFolder.Startup) + '\\' + App.PROGRAM_NAME + EXTENSION; }
		}

		/// <summary>
		/// Reads the target used by a shortcut file.
		/// </summary>
		/// <returns>
		/// The path to a file or directory.
		/// </returns>
		public static string? ReadTargetOfShortcut(string lnkFilePath)
		{
			if (!System.IO.File.Exists(lnkFilePath)) {
				return null;
			}

			string? shortcutDirectoryPath = Path.GetDirectoryName(lnkFilePath);
			string? shortcutFileName = Path.GetFileName(lnkFilePath);

			if (shortcutDirectoryPath == null || shortcutFileName == null) {
				return null;
			}

			try {
				Shell shell = new Shell();
				Shell32.Folder folder = shell.NameSpace(shortcutDirectoryPath);
				FolderItem folderItem = folder.ParseName(shortcutFileName);

				if (folderItem == null) {
					return null;
				}

				ShellLinkObject link = (ShellLinkObject)folderItem.GetLink;

				return link.Path;
			} catch (UnauthorizedAccessException) {
			}

			return null;
		}

		public static ShortcutCreationResult CreateStartupShortcut(bool overwrite)
		{
			Shortcut shortcut = new Shortcut();
			shortcut.lnkFileName = Path.GetFileNameWithoutExtension(StartupShortcutPath);
			shortcut.UseStarTradIcon();

			string? directoryPath = Path.GetDirectoryName(StartupShortcutPath);

			if (directoryPath == null) {
				return ShortcutCreationResult.CreationFailed;
			}

			return shortcut.Create(directoryPath, overwrite);
		}

		public static ShortcutCreationResult CreateDesktopShortcut(bool overwrite)
		{
			Shortcut shortcut = new Shortcut();
			shortcut.lnkFileName = "Star Citizen en français";
			shortcut.arguments.Add(App.ARGUMENT_INSTALL);
			shortcut.arguments.Add(App.ARGUMENT_LAUNCH);
			shortcut.arguments.Add(App.ARGUMENT_QUIT);
			shortcut.UseRsiStIcon();

			return shortcut.Create(DesktopDirectoryPath, overwrite);
		}

		/*
		Public
		*/

		public  void UseStarTradIcon()
		{
			this.iconFileName = "StarTrad.ico";
		}

		public  void UseRsiStIcon()
		{
			this.iconFileName = "rsist.ico";
		}

		public ShortcutCreationResult Create(string directoryPath, bool overwrite)
		{
			if (!Directory.Exists(directoryPath)) {
				Debug.WriteLine("RETURN_1 :: '" + directoryPath + "'");
				return ShortcutCreationResult.CreationFailed;
			}

			string lnkFilePath = directoryPath + '\\' + this.lnkFileName + EXTENSION;

			if (System.IO.File.Exists(lnkFilePath) && !overwrite) {
				Debug.WriteLine("RETURN_2");
				return ShortcutCreationResult.AlreadyExists;
			}

			try {
				WshShell shell = new WshShell();
				IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(lnkFilePath);

				shortcut.TargetPath = App.workingDirectoryPath + "StarTrad.exe";
				shortcut.WorkingDirectory = Path.GetDirectoryName(shortcut.TargetPath);
				shortcut.IconLocation = shortcut.TargetPath;

				if (this.iconFileName != null) {
					shortcut.IconLocation = App.workingDirectoryPath + this.iconFileName;
				}

				if (this.arguments.Count > 0) {
					shortcut.Arguments = String.Join(' ', this.arguments);
				}

				shortcut.Save();

				return ShortcutCreationResult.SuccessfulyCreated;
			} catch (Exception ex) {
				Logger.LogError(ex);
				Debug.WriteLine("RETURN_3_ERR " + ex.Message);
			}

			Debug.WriteLine("RETURN_3");
			return ShortcutCreationResult.CreationFailed;
		}
	}
}
