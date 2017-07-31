using System.Windows.Input;
using static GamanReader.StaticHelpers;

namespace GamanReader
{
	partial class MainWindow
	{
		/// <summary>
		/// Go to next page(s) on left click.
		/// </summary>
		private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_viewModel == null) return;
			GoForward(CtrlIsDown() ? 1 : _pageSize);
		}

		/// <summary>
		/// Go to previous page(s) on right click.
		/// </summary>
		private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_viewModel == null) return;
			GoBack(CtrlIsDown() ? 1 : _pageSize);
		}

		/// <summary>
		/// Go forward on mousewheel inwards and back on outwards.
		/// </summary>
		private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (_viewModel == null) return;
			if (e.Delta < 0)
			{
				if (_viewModel.FilesAhead >= 2)
				{
					GoForward(CtrlIsDown() ? 1 : _pageSize);
				}
			}
			else if (e.Delta > 0)
			{
				if (_viewModel.HasPrevious)
				{
					GoBack(CtrlIsDown() ? 1 : _pageSize);
				}
			}
		}

		/// <summary>
		/// Direction-sensitive left and right arrow keys handler.
		/// </summary>
		private void HandleKeys(object sender, KeyEventArgs e)
		{
			if (_viewModel == null) return;
			switch (e.Key)
			{
				case Key.Left:
					if (_rtlDirection)
					{
						if (_viewModel.FilesAhead >= 2)
						{
							GoForward(CtrlIsDown() ? 1 : _pageSize);
						}
					}
					else
					{
						if (_viewModel.HasPrevious)
						{
							GoBack(CtrlIsDown() ? 1 : _pageSize);
						}
					}
					return;
				case Key.Right:
					if (_rtlDirection)
					{
						if (_viewModel.HasPrevious)
						{
							GoBack(CtrlIsDown() ? 1 : _pageSize);
						}
					}
					else
					{
						if (_viewModel.FilesAhead >= 2)
						{
							GoForward(CtrlIsDown() ? 1 : _pageSize);
						}
					}
					return;
			}
		}

	}
}
