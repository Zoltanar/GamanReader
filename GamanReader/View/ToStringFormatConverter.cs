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
			if (values.Length >= 2 && values[0] is IFormattable formatValue  && values[1] is string sValue && !string.IsNullOrWhiteSpace(sValue))
			{
				return formatValue.ToString(sValue, culture);
			}
			return values[0].ToString(); ;
			}

			public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			{
				throw new NotSupportedException();
			}
		}
	
}
