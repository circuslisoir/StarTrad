using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StarTrad.View.Window
{
	/// <summary>
	/// Interaction logic for About.xaml
	/// </summary>
	public partial class About : System.Windows.Window
	{
		private View.Window.Updater? m_updaterWindow = null;

		public About()
		{
			InitializeComponent();

			this.Label_Version.Content += " " + App.assemblyFileVersion;
		}

		/*
		Private
		*/

		/// <summary>
		/// Displays a message warning about not being able to retrieve the latest release from GitHub.
		/// </summary>
		private void GitHubReleaseParsingErrorMessage()
		{
			System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
			{
				this.TextBlock_Version_Update.Text += " " + "Impossible de récupérer la dernière version.\nVérifiez manuellement sur le site ou GitHub.";
				this.TextBlock_Version_Update.Foreground = System.Windows.Media.Brushes.Red;
			}));
		}

		/// <summary>
		/// Checks if there's a new release on GitHub.
		/// </summary>
		/// <param name="messageWhenUpToDate"></param>
		private async Task CheckForNewRelease()
		{
			Supremes.Nodes.Document? doc = null;

			try {
				doc = Supremes.Dcsoup.Parse(new Uri(App.GithubRepositoryUrl + App.GITHUB_LATEST_RELEASE), 5000);
			} catch (Exception) {
				return;
			}

			if (doc == null) {
				this.GitHubReleaseParsingErrorMessage();

				return;
			}

			Supremes.Nodes.Elements links = doc.Select("a");

			if (links.Count < 1) {
				this.GitHubReleaseParsingErrorMessage();

				return;
			}

			foreach (Supremes.Nodes.Element link in links) {
				string href = link.Attr("href");
				string path = App.GITHUB_REPOSITORY + "/tree/";

				if (!href.StartsWith(path)) {
					continue;
				}

				int lastSlashPos = href.LastIndexOf('/');
				href = href.Substring(lastSlashPos + 1);

				if (href.Length < 2) {
					this.GitHubReleaseParsingErrorMessage();

					return;
				}

				string githubReleaseVersion = href.Replace(".", "") + "0";
				int release;

				if (!int.TryParse(githubReleaseVersion, out release)) {
					this.GitHubReleaseParsingErrorMessage();

					return;
				}

				// Not a newer release
				if (release <= App.AssemblyFileVersionAsNumber) {
					System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
					{
						this.TextBlock_Version_Update.Text += " " + "Pas de nouvelle version disponible.";
					}));

					return;
				}

				Supremes.Nodes.Element? releaseDiv = doc.Select("div.markdown-body").First;

				System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
				{
					this.TextBlock_Version_Update.Text += " " + "Nouvelle version " + href + " disponible !\nCliquer ici pour mettre à jour.";
					this.TextBlock_Version_Update.Foreground = System.Windows.Media.Brushes.LightGreen;
					this.TextBlock_Version_Update.TextDecorations = System.Windows.TextDecorations.Underline;
					this.TextBlock_Version_Update.Cursor = Cursors.Hand;

					m_updaterWindow = new View.Window.Updater(href);

					if (releaseDiv != null) {
						m_updaterWindow.Changelog = releaseDiv.Html;
					}
				}));

				return;
			}
		}

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

		private async void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			await Task.Run(() => this.CheckForNewRelease());
		}

		private void TextBlock_WebsiteUrl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this.OpenUrl(TextBlock_WebsiteUrl.Text);
		}

		private void TextBlock_GithubUrl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this.OpenUrl(TextBlock_GithubUrl.Text);
		}

		private void TextBlock_Version_Update_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (m_updaterWindow == null) {
				return;
			}

			m_updaterWindow.Show();
			this.Close();
		}
	}
}
