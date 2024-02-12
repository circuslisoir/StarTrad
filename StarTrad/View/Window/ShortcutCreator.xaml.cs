using System.IO;
using System.Windows.Forms;
using StarTrad.Enum;
using StarTrad.Tool;

namespace StarTrad.View.Window
{
	/// <summary>
	/// Interaction logic for ShortcutCreator.xaml
	/// </summary>
	public partial class ShortcutCreator : System.Windows.Window
	{
		public ShortcutCreator(System.Windows.Window owner)
		{
			InitializeComponent();

			this.Owner = owner;

			this.TextBox_Location.Text = Tool.Shortcut.DesktopDirectoryPath;
		}

		/*
		Event
		*/

		private void Button_Browse_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			System.Windows.Forms.FolderBrowserDialog dialog = new();

			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) {
				return;
			}

			this.TextBox_Location.Text = dialog.SelectedPath;
		}

		private void Button_CreateShortcut_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!Directory.Exists(this.TextBox_Location.Text)) {
				MessageBox.Show($"Le dossier \"{this.TextBox_Location.Text}\" n'existe pas.");

				return;
			}

			Tool.Shortcut shortcut = new();

			if (this.CheckBox_Install.IsChecked == true) {
				shortcut.arguments.Add(App.ARGUMENT_INSTALL);
			}

			if (this.CheckBox_Launch.IsChecked == true) {
				shortcut.lnkFileName = "Star Citizen en français";
				shortcut.arguments.Add(App.ARGUMENT_LAUNCH);
				shortcut.UseRsiStIcon();
			}

			if (this.RadioButton_Quit.IsChecked == true) {
				shortcut.arguments.Add(App.ARGUMENT_QUIT);
			}

			ShortcutCreationResult result = shortcut.Create(this.TextBox_Location.Text, true);

			if (result == ShortcutCreationResult.CreationFailed) {
				MessageBox.Show("Impossible de créer le raccourci.");

				return;
			}

			MessageBox.Show("Raccourci créé avec succès !");
		}

		private void RadioButton_Desktop_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!this.IsLoaded) {
				return;
			}

			this.TextBox_Location.Text = Tool.Shortcut.DesktopDirectoryPath;
			this.TextBox_Location.IsEnabled = false;
			this.Button_Browse.IsEnabled = false;
		}

		private void RadioButton_Location_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!this.IsLoaded) {
				return;
			}

			this.TextBox_Location.IsEnabled = true;
			this.Button_Browse.IsEnabled = true;
		}
	}
}
