using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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

namespace Budgeter
{
	public class NumericUpDownEventArgs : EventArgs
	{
		public double NewValue { get; set; }
		public double OldValue { get; private set; }
		public bool Abort { get; set; }

		public NumericUpDownEventArgs(double newValue, double oldValue)
		{
			NewValue = newValue;
			OldValue = oldValue;
			Abort = false;
		}
	}

	/// <summary>
	/// Interaction logic for NumericUpDown.xaml
	/// </summary>
	public partial class NumericUpDown : UserControl, INotifyPropertyChanged
	{
		int m_DecimalPlaces = 3;
		double m_Minimum = 0;
		double m_Maximum = 100;
		double m_Value = 0;

		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public event EventHandler<NumericUpDownEventArgs>? ValueChanged;

		public int DecimalPlaces
		{
			get { return m_DecimalPlaces; }
			set
			{
				m_DecimalPlaces = value;
				NotifyPropertyChanged(nameof(Value));
				NotifyPropertyChanged(nameof(ValueString));
			}
		}
		public double Increment { get; set; }
		public double Minimum { get { return m_Minimum; } set { m_Minimum = value; Value = m_Value; } }
		public double Maximum { get { return m_Maximum; } set { m_Maximum = value; Value = m_Value; } }
		public double Value
		{
			get { return m_Value; }
			set
			{
				var oldValue = m_Value;
				var newValue = Math.Min(m_Maximum, Math.Max(m_Minimum, value));

				NumericUpDownEventArgs args = new(newValue, oldValue);

				if (ValueChanged != null)
					ValueChanged(this, args);

				if (args.Abort)
					return;

				m_Value = newValue;
				NotifyPropertyChanged(nameof(Value));
				NotifyPropertyChanged(nameof(ValueString));
			}
		}
		public string ValueString
		{
			get
			{
				return String.Format(new NumberFormatInfo() { NumberDecimalDigits = DecimalPlaces }, "{0:F}", m_Value);
			}
			set => double.TryParse(value, out m_Value);
		}
		public NumericUpDown()
		{
			Increment = 1;
			DecimalPlaces = 3;

			InitializeComponent();

			Value = 0;
		}

		private void ScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
		{
			Value += (1.0 - scrollBar.Value) * Increment;

			scrollBar.Value = 1;
		}

		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Up)
			{
				Value += Increment;
				e.Handled = true;
			}
			else if (e.Key == Key.Down)
			{
				Value -= Increment;
				e.Handled = true;
			}
		}

		private void _this_MouseEnter(object sender, MouseEventArgs e)
		{
			border.BorderBrush = SystemColors.HighlightBrush;
		}

		private void _this_MouseLeave(object sender, MouseEventArgs e)
		{
			border.BorderBrush = SystemColors.ActiveBorderBrush;
		}
	}
}
