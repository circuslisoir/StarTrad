using IWshRuntimeLibrary;
using System;

namespace StarTrad.View.Window
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : System.Windows.Window
    {
        private static string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\StarTrad.lnk";

        public Settings()
        {
            InitializeComponent();

            this.CheckBox_StartWithWindows.IsChecked = IsShortcutExist(shortcutPath);
            this.TextBox_LibraryFolder.Text = Properties.Settings.Default.RsiLauncherLibraryFolder;
            this.TextBox_Channel.Text = Properties.Settings.Default.RsiLauncherChannel;
        }

        /*
		Event
		*/

        #region Events

        /// <summary>
        /// Called when checking the "Start with Windows" checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_StartWithWindows_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            string starTradPath = App.workingDirectoryPath;
            if (!IsShortcutExist(shortcutPath))
                CreateShortcut(starTradPath, shortcutPath);
        }

        /// <summary>
        /// Called when unchecking the "Start with Windows" checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_StartWithWindows_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (IsShortcutExist(shortcutPath))
                DeleteShortcut(shortcutPath);
        }

        /// <summary>
        /// Called when the channel ComboBox's dropdown gets closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_Channel_DropDownClosed(object sender, System.EventArgs e)
        {
            this.TextBox_Channel.Text = this.ComboBox_Channel.Text.Trim();
        }

        /// <summary>
        /// Called when clicking on the "Save" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Properties.Settings.Default.RsiLauncherLibraryFolder = this.TextBox_LibraryFolder.Text;
            Properties.Settings.Default.RsiLauncherChannel = this.TextBox_Channel.Text;

            Properties.Settings.Default.Save();

            this.Close();
        }

        #endregion

        private bool IsShortcutExist(string path)
        {
            return System.IO.File.Exists(path);
        }

        private void CreateShortcut(string starTradPath, string shortcutPath)
        {
            try
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = starTradPath + @"\StarTrad.exe";
                shortcut.WorkingDirectory = starTradPath;
                shortcut.IconLocation = starTradPath + @"\StarTrad.ico";

                shortcut.Save();
            }
            catch { }

        }

        private void DeleteShortcut(string path)
        {
            try
            {
                System.IO.File.Delete(path);
            }
            catch { }
        }
    }
}
