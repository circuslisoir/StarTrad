using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StarTrad.View.Control
{
	/// <summary>
	/// Interaction logic for ExternalToolItem.xaml
	/// </summary>
	public partial class ExternalToolItem : UserControl
	{
		public delegate void ItemRemovalRequestedHandler(ExternalToolItem sender);
		public event ItemRemovalRequestedHandler? OnItemRemovalRequested = null;

		public ExternalToolItem()
		{
			InitializeComponent();
		}

		/*
		Event
		*/

		private void Button_Remove_Click(object sender, RoutedEventArgs e)
		{
			if (OnItemRemovalRequested != null) {
				OnItemRemovalRequested(this);
			}
        }
    }
}
