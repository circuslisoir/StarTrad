using System.Windows;
using System.Collections.Generic;

namespace StarTrad.View.Window
{
	/// <summary>
	/// Interaction logic for ExternalTools.xaml
	/// </summary>
	public partial class ExternalTools : System.Windows.Window
	{
		public ExternalTools(System.Windows.Window owner)
		{
			InitializeComponent();

			this.Owner = owner;

			string[] executables = Properties.Settings.Default.ExternalTools.Split(';');

			foreach (string executable in executables) {
				this.AddExecutableAsItem(executable);
			}
		}

		/*
		Private
		*/

		private void AddExecutableAsItem(string executablePath)
		{
			if (string.IsNullOrWhiteSpace(executablePath)) {
				return;
			}

			Control.ExternalToolItem item = new Control.ExternalToolItem();
			item.Width = 420;
			item.TextBox_ExecutablePath.Text = executablePath;
			item.OnItemRemovalRequested += OnRemoveItem;

			this.ListBox_Executables.Items.Add(item);
		}

		/*
		Event
		*/

		private void OnRemoveItem(Control.ExternalToolItem item)
		{
			this.ListBox_Executables.Items.Remove(item);
		}

		private void Button_Add_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
			openFileDialog.Filter = "Executables|*.exe";
			openFileDialog.Multiselect = false;

			if (openFileDialog.ShowDialog() != true) {
				return;
			}

			this.AddExecutableAsItem(openFileDialog.FileName);
		}

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			List<string> executables = new List<string>();

			foreach (Control.ExternalToolItem item in this.ListBox_Executables.Items) {
				if (string.IsNullOrWhiteSpace(item.TextBox_ExecutablePath.Text)) {
					continue;
				}

				if (!System.IO.File.Exists(item.TextBox_ExecutablePath.Text)) {
					continue;
				}

				executables.Add(item.TextBox_ExecutablePath.Text);
			}

			Properties.Settings.Default.ExternalTools = string.Join(';', executables);
			Properties.Settings.Default.Save();

			this.Close();
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			foreach (Control.ExternalToolItem item in this.ListBox_Executables.Items) {
				item.Width = this.ListBox_Executables.ActualWidth - 15;
			}
		}
	}
}
