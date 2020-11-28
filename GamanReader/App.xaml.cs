using System;
using System.Windows;
using System.Windows.Threading;

namespace GamanReader
{
    public partial class App
    {
		private bool _showingError;

		private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (_showingError)
			{
				e.Handled = true;
				return;
			}
			_showingError = true;
			try
			{
				var message = $"Press Yes to continue or No to Exit.{Environment.NewLine}{e.Exception}";
				var response = MessageBox.Show(message.Substring(0, Math.Min(message.Length, 1000)),
					"Unhandled Exception", MessageBoxButton.YesNo);
				if (response == MessageBoxResult.No) Shutdown(-1);
			}
			finally
			{
				_showingError = false;
				e.Handled = true;
			}
		}
	}
}
