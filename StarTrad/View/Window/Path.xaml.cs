using System.Windows;
using StarTrad.Tool;

namespace StarTrad.View.Window
{
	/// <summary>
	/// Interaction logic for Path.xaml
	/// </summary>
	public partial class Path : System.Windows.Window
	{
		public Path()
		{
			InitializeComponent();

			this.TextBox_Path.Text = LibraryFolder.DEFAULT_LIBRARY_FOLDER_PATH;

			this.ShowDialog();
		}

		/*
		Private
		*/

		private string SeekLibraryFolderPathFrom(string path)
		{
			if (LibraryFolder.IsValidLibraryFolderPath(path)) {
				return path;
			}

			// Seek among parent directories
			string? directoryPath = path;

			while ((directoryPath = System.IO.Path.GetDirectoryName(directoryPath)) != null) {
				if (LibraryFolder.IsValidLibraryFolderPath(directoryPath)) {
					return directoryPath;
				}
			}

			return "";
		}

		/*
		Accessor
		*/

		public string? LibraryFolderPath
		{
			get { return LibraryFolder.IsValidLibraryFolderPath(this.TextBox_Path.Text) ? this.TextBox_Path.Text : null; }
		}

		/*
		Event
		*/

		private void Rectangle_Drop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

			if (files.Length < 1) {
				return;
			}

			this.TextBox_Path.Text = this.SeekLibraryFolderPathFrom(files[0]);

			if (this.TextBox_Path.Text.Length == 0) {
				MessageBox.Show("Le chemin du Library Folder n'a pas pu être obtenu à partir de ce fichier.");
			}
		}

		private void Button_Validate_Click(object sender, RoutedEventArgs e)
		{
			this.TextBox_Path.Text = this.SeekLibraryFolderPathFrom(this.TextBox_Path.Text);

			if (this.TextBox_Path.Text.Length == 0) {
				MessageBox.Show("Le chemin indiqué n'est pas valide.");
				return;
			}

			Properties.Settings.Default.RsiLauncherLibraryFolder = this.TextBox_Path.Text;
            Properties.Settings.Default.Save();

			this.Close();
		}

		private void Rectangle_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			System.Windows.Forms.FolderBrowserDialog dialog = new();

			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) {
				return;
			}

			this.TextBox_Path.Text = this.SeekLibraryFolderPathFrom(dialog.SelectedPath);

			if (this.TextBox_Path.Text.Length == 0) {
				MessageBox.Show("Le chemin du Library Folder n'a pas pu être obtenu à partir du dossier sélectionné.");
			}
		}
	}
}
