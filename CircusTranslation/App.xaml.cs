using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace CircusTranslation
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : System.Windows.Application
	{
		public App() : base()
		{
			ContextMenu contextMenu = new ContextMenu();

			MenuItem updateMenuItem = new MenuItem();
			updateMenuItem.Text = "Vérifier et installer traduction";
			updateMenuItem.Click += new EventHandler(this.UpdateMenuItem_Click);

			MenuItem updateAndLaunchMenuItem = new MenuItem();
			updateAndLaunchMenuItem.Text = "Vérifier, installer traduction et lancer";
			updateAndLaunchMenuItem.Click += new EventHandler(this.UpdateAndLaunchMenuItem_Click);

			MenuItem settingsMenuItem = new MenuItem();
			settingsMenuItem.Text = "Options avancées";
			settingsMenuItem.Click += new EventHandler(this.SettingsMenuItem_Click);

			MenuItem exitMenuItem = new MenuItem();
			exitMenuItem.Text = "Quitter";
			exitMenuItem.Click += new EventHandler(this.ExitMenuItem_Click);

			contextMenu.MenuItems.AddRange(new MenuItem[] {
				updateMenuItem,
				updateAndLaunchMenuItem,
				settingsMenuItem,
				exitMenuItem
			});

			NotifyIcon notifyIcon = new NotifyIcon(new System.ComponentModel.Container());
			notifyIcon.Icon = new Icon("icon.ico");
			notifyIcon.Visible = true;
			notifyIcon.ContextMenu = contextMenu;
		}

		/*
		Event
		*/

		/// <summary>
		/// Called when clicking on the "Update" tray menu item.
		/// </summary>
		/// <param name="Sender"></param>
		/// <param name="e"></param>
		private void UpdateMenuItem_Click(object Sender, EventArgs e)
		{
		}

		/// <summary>
		/// Called when clicking on the "Update & Launch" tray menu item.
		/// </summary>
		/// <param name="Sender"></param>
		/// <param name="e"></param>
		private void UpdateAndLaunchMenuItem_Click(object Sender, EventArgs e)
		{
		}

		/// <summary>
		/// Called when clicking on the "Settings" tray menu item.
		/// </summary>
		/// <param name="Sender"></param>
		/// <param name="e"></param>
		private void SettingsMenuItem_Click(object Sender, EventArgs e)
		{
			View.Window.Settings settingsWindow = new View.Window.Settings();
			settingsWindow.ShowDialog();
		}

		/// <summary>
		/// Called when clicking on the "Exit" tray menu item.
		/// </summary>
		/// <param name="Sender"></param>
		/// <param name="e"></param>
		private void ExitMenuItem_Click(object Sender, EventArgs e)
		{
			this.Shutdown();

			// Prevent from having a Windows messagebox about the crash
			Process process = Process.GetCurrentProcess();
			process.Kill();
			process.Dispose();
		}
	}
}
