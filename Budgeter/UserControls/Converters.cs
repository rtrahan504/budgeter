using System;
using System.Globalization;
using System.Windows.Data;

namespace Budgeter
{

	[ValueConversion(typeof(double), typeof(bool))]
	public class DoublePositive : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double doubleval)
			{
				if (doubleval > 0)
					return true;
			}
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new InvalidOperationException("Converter can only be used one way.");
		}
	}
	[ValueConversion(typeof(double), typeof(bool))]
	public class DoubleNegative : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double doubleval)
			{
				if (doubleval < 0)
					return true;
			}
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new InvalidOperationException("Converter can only be used one way.");
		}
	}

	[ValueConversion(typeof(DateTime), typeof(String))]
	public class DateConverter : IValueConverter
	{
		private const string _format = "M/d/yyyy";

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DateTime dt)
				return dt.ToString(_format);
			else
				throw new InvalidOperationException("Invalid conversion from DateTime to String.");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is String str)
				return DateTime.Parse(str, culture);
			else
				throw new InvalidOperationException("Invalid conversion from String to DateTime. Check the format of the date.");
		}
	}

	[ValueConversion(typeof(double), typeof(double))]
	public class ZoomConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double doubleval)
			{
				if (doubleval < 1)
					return 0.25 + doubleval * 0.75;
				else
					return doubleval * doubleval;
			}
			throw new InvalidOperationException("Invalid conversion double to font size.");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new InvalidOperationException("Converter can only be used one way.");
		}
	}
}
