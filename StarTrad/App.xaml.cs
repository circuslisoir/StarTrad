using StarTrad.Enum;
using StarTrad.Properties;
using StarTrad.Tool;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace StarTrad
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public const string PROGRAM_NAME = "StarTrad";
        public const string DEFAULT_RSI_LAUNCHER_NAME = "RSI Launcher";

        // Command line arguments
        public const string ARGUMENT_INSTALL = "/install";
        public const string ARGUMENT_LAUNCH  = "/launch";
        public const string ARGUMENT_SETTNGS = "/settings";
        public const string ARGUMENT_STARTUP = "/startup";
        public const string ARGUMENT_DESKTOP = "/desktop";
        public const string ARGUMENT_QUIT    = "/quit";

        // Full path to the location where this program is running
        public static readonly string workingDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string? assemblyFileVersion = App.AssemblyFileVersion;

        private static readonly ApplicationContext applicationContext = new ApplicationContext();
        private static readonly NotifyIcon notifyIcon = new NotifyIcon();

        private readonly ToolStripMenuItem installMenuItem;
        private readonly ToolStripMenuItem installAndLaunchMenuItem;
        private readonly ToolStripMenuItem uninstallMenuItem;
        private readonly ToolStripMenuItem settingsMenuItem;
        private readonly ToolStripMenuItem aboutMenuItem;

        public App() : base()
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Setup exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            System.Windows.Forms.Application.ThreadException += OnApplicationThreadException;
            System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            this.installMenuItem = new ToolStripMenuItem("Installer la traduction", null, new EventHandler(this.InstallMenuItem_Click));
            this.installAndLaunchMenuItem = new ToolStripMenuItem("Installer la traduction puis lancer le jeu", null, new EventHandler(this.InstallAndLaunchMenuItem_Click));
            this.uninstallMenuItem = new ToolStripMenuItem("Désinstaller la traduction", null, new EventHandler(this.UninstallMenuItem_Click));
            this.settingsMenuItem = new ToolStripMenuItem("Options avancées", null, new EventHandler(this.SettingsMenuItem_Click));
            this.aboutMenuItem = new ToolStripMenuItem("À propos", null, new EventHandler(this.AboutMenuItem_Click));

            // Creating the notify icon should be done before handling the command line arguments as the program would be closed 
            // immediately otherwise. However we won't make the icon visible until after handling the command line arguments.
            this.CreateNotifyIcon();

            if (!this.HandleCommandLineArguments()) {
                return;
            }

            // Single instance should only be handled after handling the command line arguments as we don't want to prevent the /install
            // and /launch arguments to be executed if there's already another instance of the program running in the background.
            this.HandleSingleInstance();
            Logger.Setup();

            notifyIcon.Visible = true;

            // Initialize update scheduler
            UpdateTranslation.OnUpdateTriggered += this.OnAutoUpdateTriggered;
            UpdateTranslation.StartAutoUpdate();

            // Start Process handler
            if (((TranslationUpdateMethod)Settings.Default.TranslationUpdateMethod == TranslationUpdateMethod.StartRsiLauncher ||
                !string.IsNullOrWhiteSpace(Settings.Default.ExternalTools)) &&
                !ProcessHandler.IsProcessHandlerRunning)
                ProcessHandler.StartProcessHandler();

            // Say hello
            App.Notify(ToolTipIcon.Info, "StarTrad démarré ! Retrouvez-le dans la zone de notification en bas à droite.");

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

        private static string? AssemblyFileVersion
        {
            get
            {
                // Reads the version number defined in Properties > Package > General > Package Version
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

                return fileVersionInfo.FileVersion; // Example: "0.9.1.0"
            }
        }

        #endregion

        #region Private

        /// <summary>
        /// Handles arguments passed to the program.
        /// </summary>
        /// <returns>
        /// False if the program should be closed after handling the command line arguments, true otherwise.
        /// </returns>
        private bool HandleCommandLineArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool keepRunning = !this.Contains(args, ARGUMENT_QUIT);

            if (this.Contains(args, ARGUMENT_STARTUP)) {
                Tool.Shortcut.CreateStartupShortcut(true);
            }

            if (this.Contains(args, ARGUMENT_DESKTOP)) {
                Tool.Shortcut.CreateDesktopShortcut(true);
            }

            if (this.Contains(args, ARGUMENT_SETTNGS)) {
                View.Window.Settings settingsWindow = new View.Window.Settings();
                settingsWindow.ShowDialog();
            }

            if (this.Contains(args, ARGUMENT_INSTALL)) {
                TranslationInstaller.Install(false, (sender, result) => {
                    if (result == Enum.ActionResult.Successful && this.Contains(args, ARGUMENT_LAUNCH)) {
                        RsiLauncherFolder.ExecuteRsiLauncher();
                    }

                    if (!keepRunning) {
                        this.ExitApplication();
                    }
                });

                return keepRunning;
            }

            // Allows the creation of a shortcut which starts the RSI launcher then leaves StarTrad open.
            if (this.Contains(args, ARGUMENT_LAUNCH)) {
                RsiLauncherFolder.ExecuteRsiLauncher();
            }

            if (!keepRunning) {
                this.ExitApplication();
            }

            return keepRunning;
        }

        /// <summary>
        /// Prevents multiple instances of the program to run at the same time.
        /// </summary>
        private void HandleSingleInstance()
        {
            bool createdNew;
            new Mutex(true, PROGRAM_NAME, out createdNew);

            if (!createdNew)
            {
                this.ExitApplication();
            }
        }

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
            cms.Items.Add(new ToolStripMenuItem("Afficher les traductions installées", null, new EventHandler(this.InstalledVersionsMenuItem_Click)));
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add(settingsMenuItem);
            cms.Items.Add(aboutMenuItem);
            cms.Items.Add(new ToolStripMenuItem("Quitter", null, new EventHandler(this.ExitMenuItem_Click)));

            notifyIcon.ContextMenuStrip = cms;
            notifyIcon.Icon = new Icon(workingDirectoryPath + "StarTrad.ico");
            notifyIcon.Text = PROGRAM_NAME;

            if (assemblyFileVersion != null) {
                notifyIcon.Text += (' ' + App.assemblyFileVersion);
            }
        }

        /// <summary>
        /// Checks if a given string list contains a certain string.
        /// </summary>
        /// <param name="strings"></param>
        /// <param name="needle"></param>
        /// <returns></returns>
        private bool Contains(string[] strings, string needle)
        {
            foreach (string str in strings)
            {
                if (str == needle)
                {
                    return true;
                }
            }

            return false;
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

        /// <summary>
        /// Terminates the application.
        /// </summary>
        private void ExitApplication()
        {
            Logger.LogInformation("Fermeture de StarTrad");

            notifyIcon.Visible = false;

            applicationContext.ExitThread();
            System.Windows.Forms.Application.Exit();
            this.Shutdown();

            Process process = Process.GetCurrentProcess();
            process.Kill();
            process.Dispose();
        }

        /// <summary>
		/// Writes a crash log file from an exception.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void WriteCrashLog(Exception e)
        {
            Logger.LogError(e.Message);
            Logger.LogError(e.Source);
            Logger.LogError(e.Data.ToString());
            Logger.LogError(e.ToString());
            Logger.LogError(e.StackTrace);

            this.ExitApplication();

            // Prevent from having a Windows messagebox about the crash
            Process process = Process.GetCurrentProcess();
            process.Kill();
            process.Dispose();
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
            Logger.LogInformation("Lancement de la recherche de mise a jour");

            this.SetMenuItemsState(false);

            TranslationInstaller.Install(false, (sender, success) => {
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

            TranslationInstaller.Install(false, (sender, result) => {
                if (result == Enum.ActionResult.Successful) RsiLauncherFolder.ExecuteRsiLauncher();
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

        private void InstalledVersionsMenuItem_Click(object? sender, EventArgs e)
        {
            LibraryFolder? libraryFolder = LibraryFolder.Make();

			if (libraryFolder == null) {
                System.Windows.Forms.MessageBox.Show("Le dossier du jeu est introuvable.");

				return;
			}

            ChannelFolder[] channelFolders = libraryFolder.EnumerateChannelFolders(false, false).ToArray();

            if (channelFolders.Length < 1) {
                System.Windows.Forms.MessageBox.Show("Aucune traduction n'est installée.");

				return;
			}

            View.Window.InstalledVersions installedVersionsWindow = new(channelFolders);
            installedVersionsWindow.ShowDialog();
        }

        /// <summary>
        /// Called when clicking on the "Settings" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsMenuItem_Click(object? sender, EventArgs e)
        {
            this.settingsMenuItem.Enabled = false;

            View.Window.Settings settingsWindow = new View.Window.Settings();
            settingsWindow.ShowDialog();

            this.settingsMenuItem.Enabled = true;
        }

        private void AboutMenuItem_Click(object? sender, EventArgs e)
        {
            this.aboutMenuItem.Enabled = false;

            View.Window.About aboutWindow = new View.Window.About();
            aboutWindow.ShowDialog();

            this.aboutMenuItem.Enabled = true;
        }

        /// <summary>
        /// Called when clicking on the "Exit" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            this.ExitApplication();
        }

        /// <summary>
        /// Called when an unhandled exception happens on the Application thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            this.WriteCrashLog(e.Exception);
        }

        /// <summary>
        /// Called when an unhandled exception happens for the current AppDomain.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            this.WriteCrashLog((Exception)e.ExceptionObject);
        }

        /// <summary>
        /// Called when the UpdateTranslation() tool triggers an automatic update of the translation.
        /// </summary>
        /// <param name="sender"></param>
        private void OnAutoUpdateTriggered(object? sender)
        {
            this.SetMenuItemsState(false);

            TranslationInstaller.Install(true, (sender, success) =>
            {
                this.SetMenuItemsState(true);
            });
        }

        #endregion
    }
}
