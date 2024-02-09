using StarTrad.Tool;
using System.Collections.Generic;

namespace StarTrad.View.Window
{
	/// <summary>
	/// Interaction logic for ChannelSelector.xaml
	/// </summary>
	public partial class ChannelSelector : System.Windows.Window
	{
		private bool okClicked = false;

		public ChannelSelector(ChannelFolder[] channelFolders, string action)
		{
			InitializeComponent();

			this.Title = action + " la traduction pour...";
			this.Button_Ok.Content = action;

            foreach (ChannelFolder channelFolder in channelFolders) {
				View.Control.ChannelSelectorItem item = new(channelFolder);

				this.ItemsControl_Channels.Items.Add(item);
            }

			this.Height = 100 * this.ItemsControl_Channels.Items.Count;
		}

		/*
		Accessor
		*/

		public ChannelFolder[] SelectedChannelFolders
		{
			get
			{
				if (!this.okClicked) {
					return [];
				}

				List<ChannelFolder> channelFolders = new List<ChannelFolder>();

				foreach (View.Control.ChannelSelectorItem item in this.ItemsControl_Channels.Items) {
					if (item.Selected) {
						channelFolders.Add(item.channelFolder);
					}	
				}

				return channelFolders.ToArray();
			}
		}

		public bool OkClicked
		{
			get { return this.okClicked; }
		}

		/*
		Event
		*/

		private void Button_Ok_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			this.okClicked = true;

			this.Close();
		}
	}
}
