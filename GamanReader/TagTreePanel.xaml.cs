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

namespace GamanReader
{
    /// <summary>
    /// Interaction logic for TagTreePanel.xaml
    /// </summary>
    public partial class TagTreePanel : UserControl
    {
		private MainWindow _mainWindow;

		public TagTreePanel(MainWindow mainWindow)
        {
			_mainWindow = mainWindow;
            InitializeComponent();
        }

		private void ItemDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var item = (sender as TreeViewItem)?.DataContext as TaggedItem;
			if (item == null) return;
			_mainWindow.LoadContainer(item.Path);
			(Parent as Window).Close();
		}
	}
}
