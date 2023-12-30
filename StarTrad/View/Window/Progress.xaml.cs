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

			this.Title += $" ({channel})";
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
	}
}
