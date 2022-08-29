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
			}
		}

		Account? SelectedAccount { get { return dataGrid_Accounts.SelectedItem as Account; } }

		public MainWindow()
		{
			InitializeComponent();

			m_CurrentFile = "{untitled}";
			NotifyPropertyChanged(nameof(DynamicTitle));

			m_Budgeter = new();
			Budgeter = m_Budgeter;
		}

		private void dataGrid_Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (SelectedAccount != null)
				NumericUpDown_DaysToForecast.Value = SelectedAccount.DaysToForecast;
		}
		private void DataGrid_Balance_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			dataGrid_Templates.SelectionChanged -= dataGrid_Templates_SelectionChanged;

			dataGrid_Templates.SelectedItems.Clear();
			foreach(var item in dataGrid_Balance.SelectedItems)
			{
				if (item is RecurringCharge charge)
					dataGrid_Templates.SelectedItems.Add(charge.Template);
			}

			dataGrid_Templates.SelectionChanged += dataGrid_Templates_SelectionChanged;
		}
		private void dataGrid_Templates_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
				else if (dataGrid_Balance.SelectedItem is RecurringCharge)
				{
					if (dataGrid_Balance.SelectedItem is RecurringCharge item)
						e.Cancel = item.Template.AmountMode != AmountModes.Variable;
				}
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
			if (e.Source == MenuItem_New)
			{
				if (System.Windows.MessageBox.Show("Are you sure you want to start a new budget?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					Budgeter = new Budget();
					CurrentFile = "{untitiled}";
				}
			}
			else if (e.Source == MenuItem_Open)
			{
				Microsoft.Win32.OpenFileDialog dialog = new()
				{
					FileName = "Budget", // Default file name
					DefaultExt = ".bgt", // Default file extension
					Filter = "Budget File (.bgt)|*.bgt" // Filter files by extension
				};

				if (dialog.ShowDialog() == true)
				{
					var newBudget = Budget.Load(dialog.FileName);
					if (newBudget != null)
					{
						Budgeter = newBudget;
						CurrentFile = dialog.FileName;
					}
				}
			}
			else if (e.Source == MenuItem_Save || e.Source == MenuItem_SaveAs)
			{
				if (e.Source == MenuItem_Save && m_CurrentFile != "")
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
			else if (e.Source == MenuItem_Exit)
			{
				Close();
			}

			else if (e.Source == MenuItem_Refresh)
			{
				(dataGrid_Accounts.SelectedItem as Account)?.UpdateEntries();
				dataGrid_Balance.Items.Refresh();
				dataGrid_Templates.Items.Refresh();
			}
			else if (e.Source == ToolButton_Account_New || e.Source == MenuItem_Account_NewAccount)
			{
				Budgeter.Accounts.Add(new Account());
			}
			else if (e.Source == ToolButton_Account_Delete || e.Source == MenuItem_Account_DeleteAccount)
			{
				if (SelectedAccount != null && System.Windows.MessageBox.Show("Are you sure you want to delete the selected account?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					Budgeter.Accounts.Remove(SelectedAccount);
				}
			}
			else if (e.Source == ToolButton_BalanceSheet_Activate || e.Source == MenuItem_BalanceSheet_Activate)
			{
				foreach (var item in dataGrid_Balance.SelectedItems)
				{
					if (item is BudgetEntry entry)
						entry.Enabled = true;
				}
				dataGrid_Balance.Items.Refresh();
			}
			else if (e.Source == ToolButton_BalanceSheet_Deactivate || e.Source == MenuItem_BalanceSheet_Deactivate)
			{
				foreach (var item in dataGrid_Balance.SelectedItems)
				{
					if (item is BudgetEntry entry)
						entry.Enabled = false;
				}
				dataGrid_Balance.Items.Refresh();
			}
			else if (e.Source == MenuItem_BalanceSheet_NewBalanceOverride || e.Source == MenuItem_BalanceSheet_NewBalanceOverride2 ||
				e.Source == MenuItem_BalanceSheet_NewCharge || e.Source == MenuItem_BalanceSheet_NewCharge2)
			{
				if (SelectedAccount != null)
				{
					DateTime date = DateTime.Now;

					var index = dataGrid_Balance.SelectedIndex;
					if (index == -1)
						index = SelectedAccount.Entries.IndexOf(SelectedAccount.Today);

					if (index >= 0 && index < SelectedAccount.Entries.Count)
						date = SelectedAccount.Entries[index].Date;

					if (e.Source == MenuItem_BalanceSheet_NewCharge || e.Source == MenuItem_BalanceSheet_NewCharge2)
					{
						var newVal = new Charge() { Date = date };
						SelectedAccount.Charges.Add(newVal);
						dataGrid_Balance.SelectedItem = newVal;
						dataGrid_Balance.Focus();
					}
					if (e.Source == MenuItem_BalanceSheet_NewBalanceOverride || e.Source == MenuItem_BalanceSheet_NewBalanceOverride2)
					{
						var newVal = new Override() { Date = date };
						SelectedAccount.BalanceOverrides.Add(newVal);
						dataGrid_Balance.SelectedItem = newVal;
						dataGrid_Balance.Focus();
					}
				}
			}
			else if (e.Source == MenuItem_BalanceSheet_Delete || e.Source == ToolButton_BalanceSheet_Delete)
			{
				List<Object> items = new();

				bool canDelete = true;

				foreach (var item in dataGrid_Balance.SelectedItems)
				{
					if (item is not Override || item is not Charge)
						canDelete = false;

					items.Add(item);
				}

				if(!canDelete)
				{
					System.Windows.MessageBox.Show("One or more of the selected entries cannot be deleted?", "Error", System.Windows.MessageBoxButton.OK);
				}
				else if (items.Count > 0 && System.Windows.MessageBox.Show("Are you sure you want to delete the selected entries?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					if (SelectedAccount != null)
					{
						foreach (var item in items)
						{
							if (item is Override overrideItem)
								SelectedAccount.BalanceOverrides.Remove(overrideItem);
							else if (item is Charge chargeItem)
								SelectedAccount.Charges.Remove(chargeItem);
						}
					}
				}
			}
			else if (e.Source == MenuItem_Template_New || e.Source == ToolButton_Template_New)
			{
				RecurringChargeTemplate val = new()
				{
					Name = "{new recurring charge}"
				};
				if (SelectedAccount != null)
					SelectedAccount.RecurringChargeTemplates.Add(val);
				dataGrid_Templates.SelectedItem = val;
				dataGrid_Templates.Focus();
			}
			else if (e.Source == MenuItem_Template_Delete || e.Source == ToolButton_Template_Delete)
			{
				if (SelectedAccount != null && System.Windows.MessageBox.Show("Are you sure you want to delete the selected entries?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					List<Object> items = new();

					foreach (var item in dataGrid_Templates.SelectedItems)
						items.Add(item);

					foreach (var item in items)
					{
						if (item is RecurringChargeTemplate recurringCharge)
							SelectedAccount.RecurringChargeTemplates.Remove(recurringCharge);
					}
				}
			}
			else
			{
				bool isWindowOpen = false;

				foreach (Window window in Application.Current.Windows)
				{
					if (window is About)
					{
						isWindowOpen = true;
						window.Activate();
					}
				}

				if (!isWindowOpen)
				{
					About newwindow = new();
					newwindow.Show();
				}
			}
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
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
			}
			catch
			{ }
			finally
			{
				key?.Close();
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
					WindowState = windowState is WindowState ? (WindowState)windowState : WindowState;

				if (Double.TryParse(key.GetValue("AccountSplitter") as string, out val))
					Grid_BalanceSheet.ColumnDefinitions[0].Width = new GridLength(val, GridUnitType.Pixel);
				if (Double.TryParse(key.GetValue("BalanceSheetSplitter") as string, out val))
					Grid_BalanceSheet.ColumnDefinitions[2].Width = new GridLength(val, GridUnitType.Star);

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
	}
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
			throw new InvalidOperationException("ItemIsToday can only be used OneWay.");
		}
	}
}
