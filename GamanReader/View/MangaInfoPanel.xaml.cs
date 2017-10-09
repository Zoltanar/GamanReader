using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using GamanReader.ViewModel;

namespace GamanReader.View
{
	/// <summary>
	/// Interaction logic for MangaInfoPanel.xaml
	/// </summary>
	public partial class MangaInfoPanel : UserControl
	{
		public MangaInfoPanel()
		{
			InitializeComponent();
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			var hyperlink = (Hyperlink) sender;
			MainWindow wnd = Window.GetWindow(this) as MainWindow;
			var searchString = hyperlink.Tag as string + ((Run) hyperlink.Inlines.FirstInline).Text;
			Debug.Assert(wnd != null, nameof(wnd) + " != null");
			((MainViewModel) wnd.DataContext).Search(searchString);
		}
	}
}
