using IWshRuntimeLibrary;
using StarTrad.Helper;
using StarTrad.Helper.ComboxList;
using StarTrad.Tool;

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

            // Bind the Checked events after the initial check so they won't be tiggered by it
            this.CheckBox_StartWithWindows.IsChecked = IsShortcutExist(shortcutPath);
            this.TextBox_LibraryFolder.Text = Properties.Settings.Default.RsiLauncherLibraryFolder;

            this.CheckBox_StartWithWindows.Checked += this.CheckBox_StartWithWindows_Checked;
            this.CheckBox_StartWithWindows.Unchecked += this.CheckBox_StartWithWindows_Unchecked;


            this.TextBox_LibraryFolder.Text = Properties.Settings.Default.RsiLauncherChannel;

            Array valeursEnum = Enum.GetValues(typeof(ChanelVersionEnum));
            foreach (ChanelVersionEnum valeur in valeursEnum)
            {
                ComboBox_Channel.Items.Add(EnumHelper.GetDescription(valeur));
            }
            this.ComboBox_Channel.Text = EnumHelper.GetDescription((ChanelVersionEnum)Enum.Parse(typeof(ChanelVersionEnum), Properties.Settings.Default.RsiLauncherChannel));

            valeursEnum = Enum.GetValues(typeof(TranslationUpdateMethodEnum));
            foreach (TranslationUpdateMethodEnum valeur in valeursEnum)
            {
                ComboBox_TranslationUpdateMethod.Items.Add(EnumHelper.GetDescription(valeur));
            }
            this.ComboBox_TranslationUpdateMethod.Text = EnumHelper.GetDescription((TranslationUpdateMethodEnum)Enum.Parse(typeof(TranslationUpdateMethodEnum), Properties.Settings.Default.TranslationUpdateMethod));

            UpdateTranslation.StopAutoUpdate();
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
        private void ComboBox_Channel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string newValue = this.ComboBox_Channel.Text.Trim();
            LoggerFactory.LogInformation($"Changement de la valeur du canal par : {newValue}");
        }

        /// <summary>
        /// Called when the Translation update methods ComboBox's dropdown gets closed.
        /// </summary>
        private void ComboBox_TranslationUpdateMethod_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string newValue = this.ComboBox_TranslationUpdateMethod.Text.Trim();
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
            Properties.Settings.Default.RsiLauncherChannel = EnumHelper.GetValueFromDescription<ChanelVersionEnum>(this.ComboBox_Channel.Text.Trim()); ;
            Properties.Settings.Default.TranslationUpdateMethod = EnumHelper.GetValueFromDescription<TranslationUpdateMethodEnum>(this.ComboBox_TranslationUpdateMethod.Text.Trim());
            Properties.Settings.Default.Save();

            UpdateTranslation.StartAutoUpdate();

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
