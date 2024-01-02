using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using StarTrad.Helper;
using StarTrad.Tool;

namespace StarTrad
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public const string PROGRAM_NAME = "StarTrad";

        // Full path to the location where this program is running
        public static string workingDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;

        private static readonly ApplicationContext applicationContext = new ApplicationContext();
        private static readonly NotifyIcon notifyIcon = new NotifyIcon();

        private readonly ToolStripMenuItem installMenuItem;
        private readonly ToolStripMenuItem installAndLaunchMenuItem;
        private readonly ToolStripMenuItem uninstallMenuItem;

        public App() : base()
        {
            LoggerFactory.Setup();
            LoggerFactory.LogInformation("Démarrage de StarTrad");

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Create notify icon
            this.installMenuItem = new ToolStripMenuItem("Installer la traduction", null, new EventHandler(this.InstallMenuItem_Click));
            this.installAndLaunchMenuItem = new ToolStripMenuItem("Installer la traduction et lancer le jeu", null, new EventHandler(this.InstallAndLaunchMenuItem_Click));
            this.uninstallMenuItem = new ToolStripMenuItem("Désinstaller la traduction", null, new EventHandler(this.UninstallMenuItem_Click));
            this.CreateNotifyIcon();

            UpdateTranslation.StartAutoUpdate();
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
            App.notifyIcon.ShowBalloonTip(2000, PROGRAM_NAME, message, icon);
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

            cms.Items.Add(installMenuItem);
            cms.Items.Add(installAndLaunchMenuItem);
            cms.Items.Add(uninstallMenuItem);
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add(new ToolStripMenuItem("Options avancées", null, new EventHandler(this.SettingsMenuItem_Click)));
            cms.Items.Add(new ToolStripMenuItem("Quitter", null, new EventHandler(this.ExitMenuItem_Click)));

            notifyIcon.ContextMenuStrip = cms;
            notifyIcon.Icon = new Icon(workingDirectoryPath + @"\StarTrad.ico");
            notifyIcon.Visible = true;
            notifyIcon.Text = PROGRAM_NAME;
        }

        /// <summary>
        /// Enables or disables the notify icon's menu items.
        /// </summary>
        /// <param name="enabled"></param>
        private void SetMenuItemsState(bool enabled)
        {
            this.installMenuItem.Enabled = enabled;
            this.installAndLaunchMenuItem.Enabled = enabled;
            this.uninstallMenuItem.Enabled = enabled;
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

            this.SetMenuItemsState(false);

            TranslationInstaller.Install(false, (sender, channelFolder) => {
                this.SetMenuItemsState(true);
            });
        }

        /// <summary>
        /// Called when clicking on the "Install & Launch" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallAndLaunchMenuItem_Click(object? sender, EventArgs e)
        {
            this.SetMenuItemsState(false);

            TranslationInstaller.Install(false, (sender, channelFolder) => {
                channelFolder.ExecuteRsiLauncher();
                this.SetMenuItemsState(true);
            });
        }

        /// <summary>
        /// Called when clicking on the "Uninstall" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UninstallMenuItem_Click(object? sender, EventArgs e)
        {
            this.SetMenuItemsState(false);
            
            TranslationInstaller.Uninstall(false);

            this.SetMenuItemsState(true);
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
