using System.Windows.Input;
using GamanReader.Model;

namespace GamanReader.View
{
	partial class MainWindow
	{
		/// <summary>
		/// Go to next page(s) on left click.
		/// </summary>
		private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			_mainModel.GoForward(StaticHelpers.CtrlIsDown());
		}

		/// <summary>
		/// Go to previous page(s) on right click.
		/// </summary>
		private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			_mainModel.GoBack(StaticHelpers.CtrlIsDown());
		}

		/// <summary>
		/// Go forward on mousewheel inwards and back on outwards.
		/// </summary>
		private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta < 0)
			{
					_mainModel.GoForward(StaticHelpers.CtrlIsDown());
			}
			else if (e.Delta > 0)
			{
					_mainModel.GoBack(StaticHelpers.CtrlIsDown());
			}
		}

		/// <summary>
		/// Direction-sensitive left and right arrow keys handler.
		/// </summary>
		private void HandleKeys(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Left:
					if (_mainModel.RtlIsChecked.Value)
					{
							_mainModel.GoForward(StaticHelpers.CtrlIsDown());
					}
					else
					{
							_mainModel.GoBack(StaticHelpers.CtrlIsDown());
					}
					return;
				case Key.Right:
					if (_mainModel.RtlIsChecked.Value)
					{
							_mainModel.GoBack(StaticHelpers.CtrlIsDown());
					}
					else
					{
							_mainModel.GoForward(StaticHelpers.CtrlIsDown());
					}
					return;
			}
		}



	}
}
