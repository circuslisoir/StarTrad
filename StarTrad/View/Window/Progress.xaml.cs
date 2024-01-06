using System.Windows.Input;

namespace StarTrad.View.Window
{
	/// <summary>
	/// Interaction logic for Progress.xaml
	/// </summary>
	public partial class Progress : System.Windows.Window
	{
		public Progress(string channel)
		{
			InitializeComponent();

			this.Label_Title.Content += $"Téléchargement de la traduction ({channel})";
		}

		/*
		Accessor
		*/

		public int ProgressBarPercentage
		{
			set { this.ProgressBar_Progress.Value = value; }
		}

		public string ProgressBarLabelText
		{
			set { this.Label_Progress.Content = value; }
		}

		/*
		Event
		*/

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left) {
				this.DragMove();
			}
		}
	}
}
