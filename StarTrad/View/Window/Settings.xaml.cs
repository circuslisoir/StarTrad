namespace StarTrad.View.Window
{
	/// <summary>
	/// Interaction logic for Settings.xaml
	/// </summary>
	public partial class Settings : System.Windows.Window
	{
		public Settings()
		{
			InitializeComponent();

			this.TextBox_LibraryFolder.Text = Properties.Settings.Default.RsiLauncherLibraryFolder;
			this.TextBox_Channel.Text = Properties.Settings.Default.RsiLauncherChannel;
		}

		/*
		Event
		*/

		/// <summary>
		/// Called when checking the "Start with Windows" checkbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CheckBox_StartWithWindows_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
		}

		/// <summary>
		/// Called when unchecking the "Start with Windows" checkbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CheckBox_StartWithWindows_Unchecked(object sender, System.Windows.RoutedEventArgs e)
		{
		}

		/// <summary>
		/// Called when the channel ComboBox's dropdown gets closed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_Channel_DropDownClosed(object sender, System.EventArgs e)
		{
			this.TextBox_Channel.Text = this.ComboBox_Channel.Text.Trim();
        }

		/// <summary>
		/// Called when clicking on the "Save" button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Save_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			Properties.Settings.Default.RsiLauncherLibraryFolder = this.TextBox_LibraryFolder.Text;
			Properties.Settings.Default.RsiLauncherChannel = this.TextBox_Channel.Text;

			Properties.Settings.Default.Save();

			this.Close();
		}
    }
}
