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
			ApplicationConfiguration.Initialize();

			this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
			this.CreateNotifyIcon();

			System.Windows.Forms.Application.Run(applicationContext);

			LoggerFactory.Setup();
			LoggerFactory.LogInformation("Démarrage de StarTrad");
		}

		/*
		Private
		*/

		/// <summary>
		/// Creates the icon and its context menu to be displayed in the system tray.
		/// </summary>
		private void CreateNotifyIcon()
		{
			ContextMenuStrip cms = new ContextMenuStrip();
			cms.Items.Add(new ToolStripMenuItem("Vérifier et installer traduction", null, new EventHandler(this.UpdateMenuItem_Click)));
			cms.Items.Add(new ToolStripMenuItem("Vérifier, installer traduction et lancer", null, new EventHandler(this.UpdateAndLaunchMenuItem_Click)));
			cms.Items.Add(new ToolStripSeparator());
			cms.Items.Add(new ToolStripMenuItem("Options avancées", null, new EventHandler(this.SettingsMenuItem_Click)));
			cms.Items.Add(new ToolStripMenuItem("Quitter", null, new EventHandler(this.ExitMenuItem_Click)));

			notifyIcon.ContextMenuStrip = cms;
			notifyIcon.Icon = new Icon(workingDirectoryPath + @"\StarTrad.ico");
			notifyIcon.Visible = true;
		}

		/*
		Event
		*/

		/// <summary>
		/// Called when clicking on the "Update" tray menu item.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UpdateMenuItem_Click(object? sender, EventArgs e)
		{
			TranslationInstaller installer = new TranslationInstaller();
			installer.InstallLatestTranslation();
		}

		/// <summary>
		/// Called when clicking on the "Update & Launch" tray menu item.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UpdateAndLaunchMenuItem_Click(object? sender, EventArgs e)
		{
			TranslationInstaller installer = new TranslationInstaller();
			installer.InstallLatestTranslation();
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
	}
}
