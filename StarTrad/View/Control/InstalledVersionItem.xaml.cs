using System.Windows.Controls;

namespace StarTrad.View.Control
{
	/// <summary>
	/// Interaction logic for InstalledVersionItem.xaml
	/// </summary>
	public partial class InstalledVersionItem : UserControl
	{
		public InstalledVersionItem()
		{
			InitializeComponent();
		}

		/*
		Setter
		*/

		public string ChannelName
		{
			set { this.Label_ChannelName.Content = value; }
		}

		public string TranslationVersion
		{
			set { this.Label_TranslationVersion.Content = value; }
		}
	}
}
