using IWshRuntimeLibrary;
using StarTrad.Helper;

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
            this.TextBox_TranslationUpdateMethod.Text = Properties.Settings.Default.TranslationUpdateMethod;
        }


        #region Events

        /// <summary>
        /// Called when checking the "Start with Windows" checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_StartWithWindows_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            LoggerFactory.LogInformation("Activation du démarrage de StarTrad avec windows");
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
            LoggerFactory.LogInformation("Désactivation du démarrage de StarTrad avec windows");
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
            string newValue = this.ComboBox_Channel.Text.Trim();
            this.TextBox_Channel.Text = newValue;
            LoggerFactory.LogInformation($"Changement de la valeur du canal par : {newValue}");
        }

        private void ComboBox_TranslationUpdateMethod_DropDownClosed(object sender, System.EventArgs e)
        {
            string newValue = this.ComboBox_TranslationUpdateMethod.Text.Trim();
            this.TextBox_TranslationUpdateMethod.Text = newValue;
            LoggerFactory.LogInformation($"Changement de la valeur de la méthode d'update par : {newValue}");
        }

        /// <summary>
        /// Called when clicking on the "Save" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoggerFactory.LogInformation("Sauvegarde et fermeture du menu des paramètres");

            Properties.Settings.Default.RsiLauncherLibraryFolder = this.TextBox_LibraryFolder.Text;
            Properties.Settings.Default.RsiLauncherChannel = this.TextBox_Channel.Text;
            Properties.Settings.Default.TranslationUpdateMethod = this.TextBox_TranslationUpdateMethod.Text;

            Properties.Settings.Default.Save();

            this.Close();
        }

        #endregion

        /// <summary>
        /// Checks if shortcut exists
        /// </summary>
        /// <param name="path">The shortcut path</param>
        /// <returns>Boolean</returns>
        private bool IsShortcutExist(string path)
        {
            return System.IO.File.Exists(path);
        }

        /// <summary>
        /// Create shortcut in folder to file
        /// </summary>
        /// <param name="starTradPath">Path to exe file of application</param>
        /// <param name="shortcutPath">Path where the shortcut who save</param>
        private void CreateShortcut(string starTradPath, string shortcutPath)
        {
            LoggerFactory.LogInformation("Création du raccourci de démarage avec windows");
            try
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = starTradPath + @"\StarTrad.exe";
                shortcut.WorkingDirectory = starTradPath;
                shortcut.IconLocation = starTradPath + @"\StarTrad.ico";

                shortcut.Save();
            }
            catch (Exception ex)
            {
                LoggerFactory.LogError(ex);
            }


        }

        /// <summary>
        /// Delete shortcut from path
        /// </summary>
        /// <param name="path">Path where located the shorcut</param>
        private void DeleteShortcut(string path)
        {
            LoggerFactory.LogInformation("Suppressions du raccourci de démarage avec windows");
            try
            {
                System.IO.File.Delete(path);
            }
            catch (Exception ex)
            {
                LoggerFactory.LogError(ex);
            }
        }
    }
}
