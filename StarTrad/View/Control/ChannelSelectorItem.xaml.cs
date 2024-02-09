using StarTrad.Enum;
using StarTrad.Tool;
using System.Windows.Controls;

namespace StarTrad.View.Control
{
	/// <summary>
	/// Interaction logic for ChannelSelectorItem.xaml
	/// </summary>
	public partial class ChannelSelectorItem : UserControl
	{
		public readonly ChannelFolder channelFolder;

		public ChannelSelectorItem(ChannelFolder channelFolder)
		{
			InitializeComponent();

			this.channelFolder = channelFolder;
			this.Label_ChannelName.Content = channelFolder.Name;
		}

		/*
		Accessor
		*/

		public bool Selected
		{
			get { return (bool)this.CheckBox_Selected.IsChecked; }
		}
	}
}
