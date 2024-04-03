using StarTrad.Enum;
using StarTrad.Tool;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Forms;

namespace StarTrad.View.Window
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : System.Windows.Window
    {
        public const string CHANNEL_ALL = "<Tous>";

        public Settings()
        {
            InitializeComponent();

            // Add ComboBox items
            this.SetupChannelsComboBox();
            this.AddComboBoxItemsFromEnum<TranslationUpdateMethod>(this.ComboBox_TranslationUpdateMethod);

            this.CheckBox_StartWithWindows.IsChecked = File.Exists(Tool.Shortcut.StartupShortcutPath);

            // Bind the Checked events after the initial IsChecked assignation so they won't be triggered by it
            this.CheckBox_StartWithWindows.Checked += this.CheckBox_StartWithWindows_Checked;
            this.CheckBox_StartWithWindows.Unchecked += this.CheckBox_StartWithWindows_Unchecked;

            this.TextBox_LibraryFolder.Text = Properties.Settings.Default.RsiLauncherLibraryFolder;

            this.ComboBox_Channel.Text = Properties.Settings.Default.RsiLauncherChannel;
            this.ComboBox_Channel.SelectionChanged += this.ComboBox_Channel_SelectionChanged;

            this.ComboBox_TranslationUpdateMethod.SelectedIndex = Properties.Settings.Default.TranslationUpdateMethod;
            this.ComboBox_TranslationUpdateMethod.SelectionChanged += this.ComboBox_TranslationUpdateMethod_SelectionChanged;

            this.CheckBox_UseNewLauncher.IsChecked = Properties.Settings.Default.LauncherName != "RSI Launcher";
            this.CheckBox_UseNewLauncher.Checked += this.CheckBox_UseNewLauncher_Checked;
            this.CheckBox_UseNewLauncher.Unchecked += this.CheckBox_UseNewLauncher_Unchecked;
        }

        #region Events

        /// <summary>
        /// Called when checking the "Start with Windows" checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_StartWithWindows_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.LogInformation("Activation du démarrage de StarTrad avec windows");
            Tool.Shortcut.CreateStartupShortcut(true);
        }

        /// <summary>
        /// Called when unchecking the "Start with Windows" checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_StartWithWindows_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.LogInformation("Désactivation du démarrage de StarTrad avec windows");

            string startupShortcutPath = Tool.Shortcut.StartupShortcutPath;

            if (!File.Exists(startupShortcutPath))
            {
                return;
            }

            try
            {
                File.Delete(startupShortcutPath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        /// <summary>
        /// Called when the channel ComboBox's dropdown gets closed.
        /// </summary>
        private void ComboBox_Channel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Logger.LogInformation($"Changement de la valeur du canal par : {this.ComboBox_Channel.Text.Trim()}");
        }

        /// <summary>
        /// Called when the Translation update methods ComboBox's dropdown gets closed.
        /// </summary>
        private void ComboBox_TranslationUpdateMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Logger.LogInformation($"Changement de la valeur de la méthode d'update par : {this.ComboBox_TranslationUpdateMethod.Text.Trim()}");
        }

        /// <summary>
        /// Called when clicking on the "CreateDesktopShortcut" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_CreateDesktopShortcut_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShortcutCreationResult result = Tool.Shortcut.CreateDesktopShortcut(true);

            switch (result)
            {
                case ShortcutCreationResult.AlreadyExists: MessageBox.Show("Le raccourci existe déjà sur le bureau."); break;
                case ShortcutCreationResult.CreationFailed: MessageBox.Show("la création du raccourci a échouée."); break;
                case ShortcutCreationResult.SuccessfulyCreated: MessageBox.Show("Raccourci créé avec succès !"); break;
            }
        }

        /// <summary>
        /// Called when clicking on the "Configure" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_ExternalTools_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExternalTools externalToolsWindow = new ExternalTools(this);
            externalToolsWindow.ShowDialog();
        }

        /// <summary>
        /// Called when clicking on the "Save" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.LogInformation("Sauvegarde et fermeture des paramètres");
            string libraryFolderPath = this.TextBox_LibraryFolder.Text.Trim();

            if (!LibraryFolder.IsValidLibraryFolderPath(libraryFolderPath))
            {
                MessageBox.Show($"Le dossier \"{libraryFolderPath}\" ne semble pas être le chemin correct vers le Library Folder.");

                return;
            }

            Properties.Settings.Default.RsiLauncherLibraryFolder = libraryFolderPath;
            Properties.Settings.Default.RsiLauncherChannel = this.ComboBox_Channel.Text;
            Properties.Settings.Default.TranslationUpdateMethod = (byte)(TranslationUpdateMethod)((ComboBoxItem)this.ComboBox_TranslationUpdateMethod.SelectedItem).Tag;

            Properties.Settings.Default.Save();

            UpdateTranslation.ReloadAutoUpdate();

            this.Close();
        }

        /// <summary>
        /// Use the new launcher checkbox checked event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_UseNewLauncher_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.LogInformation("Utilisation du nouveau launcher RSI V2");
            Properties.Settings.Default.LauncherName = "RSI RC Launcher";
        }

        /// <summary>
        /// Use the new launcher checkbox unchecked event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_UseNewLauncher_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.LogInformation("Utilisation du launcher RSI V1");
            Properties.Settings.Default.LauncherName = "RSI Launcher";
        }

        #endregion

        /// <summary>
        /// Adds all the values of an Enum as items for a ComboBox.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="comboBox"></param>
        /// <param name="e"></param>
        private void AddComboBoxItemsFromEnum<TEnum>(System.Windows.Controls.ComboBox comboBox)
        {
            foreach (System.Enum value in System.Enum.GetValues(typeof(TEnum)))
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Tag = value;
                item.Content = EnumHelper.GetDescription(value);

                comboBox.Items.Add(item);
            }
        }

        /// <summary>
        /// Adds items to the channel ComboBox depending on the existing channel directories.
        /// Also update the channel in settings if the existing value isn't valid.
        /// </summary>
        private void SetupChannelsComboBox()
        {
            string[] channelDirectoryPaths = LibraryFolder.ListAvailableChannelFolderPaths();

            this.ComboBox_Channel.Items.Add(CHANNEL_ALL);

            foreach (string channelDirectoryPath in channelDirectoryPaths)
            {
                this.ComboBox_Channel.Items.Add(System.IO.Path.GetFileName(channelDirectoryPath));
            }

            if (channelDirectoryPaths.Length < 1)
            {
                Properties.Settings.Default.RsiLauncherChannel = "";
                this.ComboBox_Channel.IsEnabled = false;
                this.Label_ChannelNotFound.Content = "Aucun canal trouvé";
            }
            else if (!this.ComboBox_Channel.Items.Contains(Properties.Settings.Default.RsiLauncherChannel))
            {
                Properties.Settings.Default.RsiLauncherChannel = System.IO.Path.GetFileName(channelDirectoryPaths[0]);
            }

            Properties.Settings.Default.Save();
        }

        private void Button_ShortcutOptions_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            View.Window.ShortcutCreator window = new(this);
            window.ShowDialog();
        }

        private void Button_LibraryFolderBrowse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            View.Window.Path window = new(this);
            string? libraryFolderPath = window.LibraryFolderPath;

            if (libraryFolderPath != null)
            {
                this.TextBox_LibraryFolder.Text = libraryFolderPath;
            }
        }
    }
}
