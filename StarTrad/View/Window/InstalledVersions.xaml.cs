using StarTrad.Tool;

namespace StarTrad.View.Window
{
	/// <summary>
	/// Interaction logic for InstalledVersions.xaml
	/// </summary>
	public partial class InstalledVersions : System.Windows.Window
	{
		public InstalledVersions(ChannelFolder[] channelFolders)
		{
			InitializeComponent();

            foreach (ChannelFolder channelFolder in channelFolders) {
				TranslationVersion? translationVersion = channelFolder.GetInstalledTranslationVersion();

				View.Control.InstalledVersionItem item = new();
				item.ChannelName = channelFolder.Name;
				item.TranslationVersion = translationVersion == null ? "traduction non installée" : translationVersion.FullVersionNumber;

				this.ItemsControl_InstalledVersions.Items.Add(item);
            }

			this.Height = 100 * this.ItemsControl_InstalledVersions.Items.Count;
		}
	}
}
