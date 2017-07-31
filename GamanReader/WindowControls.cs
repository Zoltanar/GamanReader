using System;
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
			_mainModel.GoForward(CtrlIsDown());
		}

		/// <summary>
		/// Go to previous page(s) on right click.
		/// </summary>
		private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			_mainModel.GoBack(CtrlIsDown());
		}

		/// <summary>
		/// Go forward on mousewheel inwards and back on outwards.
		/// </summary>
		private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta < 0)
			{
					_mainModel.GoForward(CtrlIsDown());
			}
			else if (e.Delta > 0)
			{
					_mainModel.GoBack(CtrlIsDown());
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
							_mainModel.GoForward(CtrlIsDown());
					}
					else
					{
							_mainModel.GoBack(CtrlIsDown());
					}
					return;
				case Key.Right:
					if (_mainModel.RtlIsChecked.Value)
					{
							_mainModel.GoBack(CtrlIsDown());
					}
					else
					{
							_mainModel.GoForward(CtrlIsDown());
					}
					return;
			}
		}



	}
}
