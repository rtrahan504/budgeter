using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Budgeter
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		readonly String RegistryPath_CurrentFile = @"SOFTWARE\Budgeter";

		bool m_BudgetModified = false;
		bool BudgetModified
		{
			get { return m_BudgetModified; }
			set
			{
				if (m_BudgetModified != value)
				{
					m_BudgetModified = value;
					NotifyPropertyChanged(nameof(DynamicTitle));
				}
			}
		}
		Account? SelectedAccount { get { return dataGrid_Accounts.SelectedItem as Account; } }

		Budget m_Budgeter;
		Budget Budgeter
		{
			get { return m_Budgeter; }
			set
			{
				if (m_Budgeter != value)
					BudgetModified = false;

				m_Budgeter = value;

				dataGrid_Accounts.ItemsSource = m_Budgeter.Accounts;
				dataGrid_Accounts.SelectedIndex = 0;

				m_Budgeter.PropertyChanged += Budgeter_PropertyChanged;

				label_AccountTotal.SetBinding(Label.ContentProperty, new Binding(nameof(Budget.AccountsTotal)) { Source = m_Budgeter });
			}
		}

		private void Budgeter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			BudgetModified = true;
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		String m_CurrentFile = "";
		public String CurrentFile
		{
			get { return m_CurrentFile; }
			set
			{
				var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegistryPath_CurrentFile);
				key.SetValue("CurrentFile", value == "{untitled}" ? "" : value);
				key.Close();

				m_CurrentFile = value;
				NotifyPropertyChanged(nameof(DynamicTitle));
			}
		}
		public String DynamicTitle
		{
			get
			{
				if (CurrentFile == null || CurrentFile == "")
					return "Budgeter";
				else
					return "Budgeter - " + System.IO.Path.GetFileName(CurrentFile) + (BudgetModified ? "*" : "");
			}
		}


		public MainWindow()
		{
			m_CurrentFile = "{untitled}";
			NotifyPropertyChanged(nameof(DynamicTitle));

			m_Budgeter = new();

			InitializeComponent();

			Budgeter = m_Budgeter;
		}

		private void DataGrid_Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (SelectedAccount != null)
				NumericUpDown_DaysToForecast.Value = SelectedAccount.DaysToForecast;
		}
		private void DataGrid_Balance_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			dataGrid_Templates.SelectionChanged -= DataGrid_Templates_SelectionChanged;

			dataGrid_Templates.SelectedItems.Clear();
			foreach(var item in dataGrid_Balance.SelectedItems)
			{
				if (item is RecurringCharge charge)
					dataGrid_Templates.SelectedItems.Add(charge.Template);
			}

			dataGrid_Templates.SelectionChanged += DataGrid_Templates_SelectionChanged;
		}
		private void DataGrid_Templates_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			dataGrid_Balance.SelectionChanged -= DataGrid_Balance_SelectionChanged;

			if (SelectedAccount != null)
			{
				var vals = (dataGrid_Templates.SelectedItem as RecurringChargeTemplate)?.GetRecurringCharges(SelectedAccount.DaysToForecast);
				if (vals != null)
				{
					dataGrid_Balance.SelectedItems.Clear();
					foreach (var item in vals)
						dataGrid_Balance.SelectedItems.Add(item);
				}
			}

			dataGrid_Balance.SelectionChanged += DataGrid_Balance_SelectionChanged;
		}
		private void DataGrid_Balance_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
		{
			if (e.Column == BalanceEnabledColumn)
			{
			}
			else if (e.Column == BalanceDateColumn)
			{
				if (dataGrid_Balance.SelectedItem is Today)
					e.Cancel = true;
				else if (dataGrid_Balance.SelectedItem is RecurringCharge)
					e.Cancel = true;
			}
			else if (e.Column == BalanceNameColumn)
			{
				if (dataGrid_Balance.SelectedItem is Today)
					e.Cancel = true;
				else if (dataGrid_Balance.SelectedItem is RecurringCharge)
					e.Cancel = true;
			}
			else if (e.Column == BalanceAmountColumn)
			{
				if (dataGrid_Balance.SelectedItem is Today)
					e.Cancel = true;
			}
			else if (e.Column == BalanceBalanceColumn)
			{
				e.Cancel = true;
			}
		}
		private void DataGrid_Balance_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
		{
			Application.Current.Dispatcher.BeginInvoke(
			  DispatcherPriority.Background,
			  () =>
			  {
				  try
				  {
					  if (e.Column == BalanceDateColumn)
						  (dataGrid_Accounts.SelectedItem as Account)?.UpdateEntries();
					  dataGrid_Balance.Items.Refresh();
					  dataGrid_Accounts.Items.Refresh();

					  BudgetModified = true;
				  }
				  catch { }
			  });
		}
		private void DataGrid_Templates_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
		{
			Application.Current.Dispatcher.BeginInvoke(
			  DispatcherPriority.Background,
			  () =>
			  {
				  (dataGrid_Accounts.SelectedItem as Account)?.UpdateEntries();
				  dataGrid_Balance.Items.Refresh();
				  dataGrid_Accounts.Items.Refresh();

				  BudgetModified = true;
			  });
		}


		private void OnMenuClick(object sender, RoutedEventArgs e)
		{
			if (e.Source is not MenuItem menuItem)
				return;

			if (menuItem.Tag is not String menuItemTag)
				return;

			if (menuItemTag == "File_New")
			{
				if (System.Windows.MessageBox.Show("Are you sure you want to start a new budget?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					Budgeter = new Budget();
					CurrentFile = "{untitled}";
				}
			}
			else if (menuItemTag == "File_Open")
			{
				Microsoft.Win32.OpenFileDialog dialog = new()
				{
					FileName = "Budget", // Default file name
					DefaultExt = ".bgt", // Default file extension
					Filter = "Budget File (.bgt)|*.bgt" // Filter files by extension
				};

				if (dialog.ShowDialog() == true)
				{
					try
					{
						var newBudget = Budget.Load(dialog.FileName);
						if (newBudget != null)
						{
							Budgeter = newBudget;
							CurrentFile = dialog.FileName;
						}
					}
					catch(Exception ex)
					{
						MessageBox.Show("An error occurred.\n" + ex.Message, "Error", MessageBoxButton.OK);
					}
				}
			}
			else if (menuItemTag == "File_Save" || menuItemTag == "File_SaveAs")
			{
				SaveBudget(menuItemTag == "File_SaveAs");
			}
			else if (menuItemTag == "Program_Exit")
			{
				Close();
			}
			else if (menuItemTag == "Account_NewAccount")
			{
				Budgeter.Accounts.Add(new Account());
			}
			else if (menuItemTag == "Account_DeleteAccount")
			{
				if (SelectedAccount != null && System.Windows.MessageBox.Show("Are you sure you want to delete the selected account?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					Budgeter.Accounts.Remove(SelectedAccount);
				}
			}
			else if (menuItemTag == "BalanceSheet_Refresh")
			{
				(dataGrid_Accounts.SelectedItem as Account)?.UpdateEntries();
				dataGrid_Balance.Items.Refresh();
				dataGrid_Templates.Items.Refresh();
			}
			else if (menuItemTag == "BalanceSheet_Activate")
			{
				foreach (var item in dataGrid_Balance.SelectedItems)
				{
					if (item is BudgetEntry entry)
						entry.Enabled = true;
				}
				dataGrid_Balance.Items.Refresh();
			}
			else if (menuItemTag == "BalanceSheet_Deactivate")
			{
				foreach (var item in dataGrid_Balance.SelectedItems)
				{
					if (item is BudgetEntry entry)
						entry.Enabled = false;
				}
				dataGrid_Balance.Items.Refresh();
			}
			else if (menuItemTag == "BalanceSheet_NewBalanceOverride" || menuItemTag == "BalanceSheet_NewCharge")
			{
				if (SelectedAccount != null)
				{
					DateTime date = DateTime.Now;

					var index = dataGrid_Balance.SelectedIndex;
					if (index == -1)
						index = SelectedAccount.Entries.IndexOf(SelectedAccount.Today);

					if (index >= 0 && index < SelectedAccount.Entries.Count)
						date = SelectedAccount.Entries[index].Date;

					if (menuItemTag == "BalanceSheet_NewCharge")
					{
						var newVal = new Charge() { Date = date };
						SelectedAccount.Charges.Add(newVal);
						dataGrid_Balance.SelectedItem = newVal;
						dataGrid_Balance.Focus();
					}
					if (menuItemTag == "BalanceSheet_NewBalanceOverride")
					{
						var newVal = new Override() { Date = date };
						SelectedAccount.BalanceOverrides.Add(newVal);
						dataGrid_Balance.SelectedItem = newVal;
						dataGrid_Balance.Focus();
					}
				}
			}
			else if (menuItemTag == "BalanceSheet_Delete")
			{
				List<Object> items = new();
				HashSet<string> messages = new();
				foreach (var item in dataGrid_Balance.SelectedItems)
				{
					if (item is not BudgetEntry entry)
						continue;
					else if (item is Today)
					{ }
					else
					{
						messages.Add(entry.Name);
						items.Add(item);
					}
				}

				string message = "";
				foreach (var item in messages)
					message += (message.Length != 0 ? ", " : "") + item;

				if (items.Count > 0 && System.Windows.MessageBox.Show("Are you sure you want to delete the selected entries?\n\n" + message, "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					if (SelectedAccount != null)
					{
						foreach (var item in items)
						{
							if (item is Override overrideItem)
								SelectedAccount.BalanceOverrides.Remove(overrideItem);
							else if (item is Charge chargeItem)
								SelectedAccount.Charges.Remove(chargeItem);
							else if (item is RecurringCharge recurring)
								SelectedAccount.Entries.Remove(recurring);
						}
					}
				}
			}
			else if (menuItemTag == "BalanceSheet_ResetAmount")
			{
				foreach (var item in dataGrid_Balance.SelectedItems)
				{
					if (item is RecurringCharge charge)
						charge.ResetAmount();
					else if(item is BudgetEntry entry)
						entry.Amount = null;
				}
				dataGrid_Balance.Items.Refresh();
				dataGrid_Accounts.Items.Refresh();

				BudgetModified = true;
			}
			else if (menuItemTag == "RecurringTransactions_New")
			{
				RecurringChargeTemplate val = new()
				{
					Name = "{new recurring charge}"
				};
				SelectedAccount?.RecurringChargeTemplates.Add(val);
				dataGrid_Templates.SelectedItem = val;
				dataGrid_Templates.Focus();
			}
			else if (menuItemTag == "RecurringTransactions_Delete")
			{
				List<RecurringChargeTemplate> items = new();
				HashSet<string> messages = new();
				foreach (var item in dataGrid_Templates.SelectedItems)
				{
					if (item is not RecurringChargeTemplate entry)
						continue;
					else
					{
						messages.Add(entry.Name);
						items.Add(entry);
					}
				}

				string message = "";
				foreach (var item in messages)
					message += (message.Length != 0 ? ", " : "") + item;

				if (SelectedAccount != null && System.Windows.MessageBox.Show("Are you sure you want to delete the selected entries?\n\n" + message, "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					foreach (RecurringChargeTemplate item in items)
					{
						SelectedAccount.RecurringChargeTemplates.Remove(item);
					}
				}
			}
			else if (menuItemTag == "About")
			{
				About newwindow = new()
				{
					Owner = this
				};

				if (toggleButton_DarkMode.IsChecked == false)
				{
					var rd = new System.Windows.ResourceDictionary
					{
						Source = new System.Uri("pack://application:,,,/Selen.Wpf.SystemStyles;component/Styles.xaml")
					};
					newwindow.Resources.MergedDictionaries.Add(rd);
				}

				newwindow.ShowDialog();
			}
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (m_BudgetModified)
			{
				var res = MessageBox.Show("Save the changes to the current budget?", "Budget Modified", MessageBoxButton.YesNoCancel);
				if (res == MessageBoxResult.Cancel)
				{
					e.Cancel = true;
					return;
				}
				else if (res == MessageBoxResult.Yes)
				{
					SaveBudget(false);
				}
			}

			Microsoft.Win32.RegistryKey? key = null;
			try
			{
				key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegistryPath_CurrentFile);
				if (key == null)
					return;
				key.SetValue("WindowWidth", Width);
				key.SetValue("WindowHeight", Height);
				key.SetValue("WindowState", WindowState.ToString());
				key.SetValue("WindowX", Left);
				key.SetValue("WindowY", Top);
				key.SetValue("AccountSplitter", Grid_BalanceSheet.ColumnDefinitions[0].Width.Value.ToString());
				key.SetValue("BalanceSheetSplitter", (Grid_BalanceSheet.ColumnDefinitions[2].Width.Value / Grid_BalanceSheet.ColumnDefinitions[4].Width.Value).ToString());
				key.SetValue("ColorMode", toggleButton_DarkMode.IsChecked == true ? "Light" : "Dark");
			}
			catch
			{ }
			finally
			{
				key?.Close();
			}
		}

		void SaveBudget(bool forceSaveAs)
		{
			if (!forceSaveAs && m_CurrentFile != "")
			{
				Budgeter.Save(m_CurrentFile);
				BudgetModified = false;
			}
			else
			{
				Microsoft.Win32.SaveFileDialog dialog = new()
				{
					FileName = "Budget", // Default file name
					DefaultExt = ".bgt", // Default file extension
					Filter = "Budget File (.bgt)|*.bgt" // Filter files by extension
				};

				if (dialog.ShowDialog() == true)
				{
					Budgeter.Save(dialog.FileName);
					BudgetModified = false;

					CurrentFile = dialog.FileName;
				}
			}
		}

		private void Window_SourceInitialized(object sender, EventArgs e)
		{
			Microsoft.Win32.RegistryKey? key = null;
			try
			{
				key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryPath_CurrentFile);
				if (key == null)
					return;
				
				var file = key.GetValue("CurrentFile") as string ?? "";

				double val = Width;
				if (Double.TryParse(key.GetValue("WindowWidth")?.ToString(), out val))
					Width = val;
				val = Height;
				if (Double.TryParse(key.GetValue("WindowHeight")?.ToString(), out val))
					Height = val;
				val = Left;
				if (Double.TryParse(key.GetValue("WindowX")?.ToString(), out val))
					Left = val;
				val = Top;
				if (Double.TryParse(key.GetValue("WindowY")?.ToString(), out val))
					Top = val;

				object? windowState = WindowState;
				if (WindowState.TryParse(typeof(WindowState), key.GetValue("WindowState")?.ToString(), true, out windowState))
					WindowState = windowState is WindowState state ? state : WindowState;

				if (Double.TryParse(key.GetValue("AccountSplitter") as string, out val))
					Grid_BalanceSheet.ColumnDefinitions[0].Width = new GridLength(val, GridUnitType.Pixel);
				if (Double.TryParse(key.GetValue("BalanceSheetSplitter") as string, out val))
					Grid_BalanceSheet.ColumnDefinitions[2].Width = new GridLength(val, GridUnitType.Star);

				toggleButton_DarkMode.IsChecked = key.GetValue("ColorMode")?.ToString() == "Light";

				var newBudget = Budget.Load(file);
				if (newBudget != null)
				{
					m_CurrentFile = file;
					NotifyPropertyChanged(nameof(DynamicTitle));
					Budgeter = newBudget;
				}
			}
			catch
			{ }
			finally
			{
				key?.Close();
			}
		}

		private void NumericUpDown_ValueChanged(object sender, NumericUpDownEventArgs e)
		{
			if(SelectedAccount != null)
				SelectedAccount.DaysToForecast = (int)e.NewValue;
		}

		private void ToggleButton_Checked(object sender, RoutedEventArgs e)
		{
			foreach (var dict in Resources.MergedDictionaries)
			{
				if (dict != null && dict.Source.OriginalString == "pack://application:,,,/Selen.Wpf.SystemStyles;component/Styles.xaml")
				{
					Resources.MergedDictionaries.Remove(dict);

					UpdateLayout();

					break;
				}
			}
		}

		private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
		{
			var rd = new System.Windows.ResourceDictionary()
			{
				Source = new System.Uri("pack://application:,,,/Selen.Wpf.SystemStyles;component/Styles.xaml")
			};
			Resources.MergedDictionaries.Add(rd);
		}

		private void Label_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			m_Slider_Name.Value = 1;
		}
	}
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
