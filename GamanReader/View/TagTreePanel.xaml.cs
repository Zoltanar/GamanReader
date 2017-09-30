using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GamanReader.Model;

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
			if(window == null) throw new Exception("MainWindow not found.");
			window.LoadContainer(item.Path);
			Visibility = Visibility.Collapsed;
		}
	}
}
