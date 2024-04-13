using System.Diagnostics;
using System.Windows.Input;

namespace StarTrad.View.Window
{
	/// <summary>
	/// Interaction logic for About.xaml
	/// </summary>
	public partial class About : System.Windows.Window
	{
		public About()
		{
			InitializeComponent();

			this.Label_Version.Content += " " + App.assemblyFileVersion;
		}

		/*
		Private
		*/

		private void OpenUrl(string url)
		{
			Process.Start(new ProcessStartInfo {
				FileName = url,
				UseShellExecute = true
			});
		}

		/*
		Event
		*/

		private void TextBlock_WebsiteUrl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this.OpenUrl(TextBlock_WebsiteUrl.Text);
		}

		private void TextBlock_GithubUrl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this.OpenUrl(TextBlock_GithubUrl.Text);
		}
	}
}
