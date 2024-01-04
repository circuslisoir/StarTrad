using System;
using System.IO;
using StarTrad.Helper;
using IWshRuntimeLibrary;
using Shell32;

namespace StarTrad.Tool
{
	/// <summary>
	/// Creates or reads shortcut files.
	/// </summary>
	internal class ShortcutTool
	{
		/// <summary>
        /// Create shortcut in folder to file
        /// </summary>
        /// <param name="targetFilePath">Absolute path to the file targeted by the shortcut</param>
        /// <param name="shortcutPath">Path where the shortcut will be saved</param>
        /// <param name="arguments">One or more command line arguments to be added to the shortcut's target</param>
        public static bool CreateShortcut(string targetFilePath, string shortcutPath, string[]? arguments = null)
        {
            LoggerFactory.LogInformation("Création d'un raccourci vers \"" + targetFilePath + "\" à l'emplacement \"" + shortcutPath + '"');

            try {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = targetFilePath;
                shortcut.WorkingDirectory = Path.GetFileName(targetFilePath);
                shortcut.IconLocation = targetFilePath;

                if (arguments != null) {
                    shortcut.Arguments = String.Join(' ', arguments);
                }

                shortcut.Save();

                return true;
            } catch (Exception ex) {
                LoggerFactory.LogError(ex);
            }

            return false;
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
	}
}
