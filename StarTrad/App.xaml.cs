using System.IO;
using System.Windows;
using StarTrad.Helper;
using StarTrad.Tool;

namespace StarTrad
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        // Full path to the location where this program is running
        public static string workingDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;

        private static ApplicationContext applicationContext = new ApplicationContext();
        private static NotifyIcon notifyIcon = new NotifyIcon();

        public App() : base()
        {
            LoggerFactory.Setup();
            LoggerFactory.LogInformation("Démarrage de StarTrad");

            ApplicationConfiguration.Initialize();

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            this.CreateNotifyIcon();

            System.Windows.Forms.Application.Run(applicationContext);
        }

		#region Static

        /// <summary>
        /// Displays a message from the notify icon.
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="message"></param>
        public static void Notify(ToolTipIcon icon, string message)
        {
            App.notifyIcon.ShowBalloonTip(2000, "StarTrad", message, icon);
        }

		#endregion

		#region Private

		/// <summary>
		/// Creates the icon and its context menu to be displayed in the system tray.
		/// </summary>
		private void CreateNotifyIcon()
        {
            ContextMenuStrip cms = new ContextMenuStrip();

            ToolStripMenuItem titleMenuItem = new ToolStripMenuItem("Traduction :");
            titleMenuItem.Enabled = false;

            cms.Items.Add(titleMenuItem);
            cms.Items.Add(new ToolStripMenuItem("Installer", null, new EventHandler(this.InstallMenuItem_Click)));
            cms.Items.Add(new ToolStripMenuItem("Installer et lancer", null, new EventHandler(this.InstallAndLaunchMenuItem_Click)));
            cms.Items.Add(new ToolStripMenuItem("Désinstaller", null, new EventHandler(this.UninstallMenuItem_Click)));
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add(new ToolStripMenuItem("Options avancées", null, new EventHandler(this.SettingsMenuItem_Click)));
            cms.Items.Add(new ToolStripMenuItem("Quitter", null, new EventHandler(this.ExitMenuItem_Click)));

            notifyIcon.ContextMenuStrip = cms;
            notifyIcon.Icon = new Icon(workingDirectoryPath + @"\StarTrad.ico");
            notifyIcon.Visible = true;
            notifyIcon.Text = "StarTrad";
        }

        #endregion

        #region Event

        /// <summary>
        /// Called when clicking on the "Install" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallMenuItem_Click(object? sender, EventArgs e)
        {
            LoggerFactory.LogInformation("Lancement de la recherche de mise a jour");
            TranslationInstaller.Install(false);
        }

        /// <summary>
        /// Called when clicking on the "Install & Launch" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallAndLaunchMenuItem_Click(object? sender, EventArgs e)
        {
            TranslationInstaller.Install(false, (sender, channelFolder) => {
                channelFolder.ExecuteRsiLauncher();
            });
        }

        /// <summary>
        /// Called when clicking on the "Uninstall" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UninstallMenuItem_Click(object? sender, EventArgs e)
        {
            bool success = TranslationInstaller.Uninstall();

            if (success) {
                App.Notify(ToolTipIcon.Info, "Traduction désinstallée avec succès !");    
            } else {
                App.Notify(ToolTipIcon.Warning, "La traduction n'a pas pu être désinstallée.");
            }
        }

        /// <summary>
        /// Called when clicking on the "Settings" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsMenuItem_Click(object? sender, EventArgs e)
        {
            LoggerFactory.LogInformation("Ouverture des paramètres");
            View.Window.Settings settingsWindow = new View.Window.Settings();
            settingsWindow.ShowDialog();
        }

        /// <summary>
        /// Called when clicking on the "Exit" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            LoggerFactory.LogInformation("Fermeture de StarTrad");

            applicationContext.ExitThread();
            notifyIcon.Visible = false;

            System.Windows.Forms.Application.Exit();
        }

        #endregion
    }
}
