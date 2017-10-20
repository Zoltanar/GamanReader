using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GamanReader.Model.Database;
using GamanReader.ViewModel;

namespace GamanReader.View
{
	/// <summary>
	/// Interaction logic for TagTreePanel.xaml
	/// </summary>
	public partial class TagTreePanel : UserControl
	{
		public TagTreePanel()
		{
			InitializeComponent();
		}

		private void ItemDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			if (!((sender as TreeViewItem)?.DataContext is TaggedItem item)) return;
			var window = (MainWindow) Window.GetWindow(this);
			//window.LoadContainer(item.Path, item.IsFolder); //todo load mangainfo, remove taggeditem
		}

		public void Refresh()
		{
			((TagTreeViewModel)DataContext).Refresh();
		}
	}
}
