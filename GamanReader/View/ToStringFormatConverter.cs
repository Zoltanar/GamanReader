using System;
using System.Globalization;
using System.Windows.Data;

namespace GamanReader.View
{
	/// <summary>
	 /// Allow a binding where the StringFormat is also bound to a property (and can vary).
	 /// </summary>
		public class ToStringFormatConverter : IMultiValueConverter
		{
			public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{

			if (values.Length == 1) return values[0].ToString();
			if (values.Length >= 2 && values[0] is IFormattable formatValue) return formatValue.ToString((string)values[1], culture);
			return null;
			}

			public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			{
				throw new NotSupportedException();
			}
		}
	
}
