using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Budgeter;
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
					return "Budgeter - " + System.IO.Path.GetFileName(CurrentFile);
			}
		}

		Budget m_Budgeter = new();
		Budget Budgeter
		{
			get { return m_Budgeter; }
			set
			{
				m_Budgeter = value;

				dataGrid_Balance.ItemsSource = m_Budgeter.Entries;
				dataGrid_Templates.ItemsSource = m_Budgeter.RecurringChargeTemplates;
			}
		}

		public MainWindow()
		{
			InitializeComponent();

			CurrentFile = "{untitiled}";
			Budgeter = new Budget();
		}


		private void DataGrid_Templates_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
		{
			Budgeter.UpdateEntries();
			dataGrid_Balance.Items.Refresh();
		}

		private void DataGrid_Balance_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			dataGrid_Templates.SelectedItem = (dataGrid_Balance.SelectedItem as RecurringCharge)?.Template;
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
						  Budgeter.UpdateEntries();
					  dataGrid_Balance.Items.Refresh();
				  }
				  catch { }
			  });
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
					Budgeter.Save(m_CurrentFile);
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
				Budgeter.UpdateEntries();
				dataGrid_Balance.Items.Refresh();
				dataGrid_Templates.Items.Refresh();
			}
			else if (e.Source == MenuItem_ActivateSelected)
			{
				foreach (var item in dataGrid_Balance.SelectedItems)
				{
					if (item is BudgetEntry @entry)
						@entry.Enabled = true;
				}
				dataGrid_Balance.Items.Refresh();
			}
			else if (e.Source == MenuItem_DeactivateSelected)
			{
				foreach (var item in dataGrid_Balance.SelectedItems)
				{
					if (item is BudgetEntry @entry)
						@entry.Enabled = false;
				}
				dataGrid_Balance.Items.Refresh();
			}
			else if (e.Source == MenuItem_AddBalanceOverride || e.Source == MenuItem_AddCharge)
			{
				DateTime date = DateTime.Now;

				var index = dataGrid_Balance.SelectedIndex;
				if (index == -1)
					index = Budgeter.Entries.IndexOf(Budgeter.Today);

				if (index >= 0 && index < Budgeter.Entries.Count)
					date = Budgeter.Entries[index].Date;

				if (e.Source == MenuItem_AddCharge)
					Budgeter.Charges.Add(new Charge() { Date = date });
				if (e.Source == MenuItem_AddBalanceOverride)
					Budgeter.BalanceOverrides.Add(new Override() { Date = date });
			}
			else if (e.Source == MenuItem_DeleteSelected)
			{
				if (System.Windows.MessageBox.Show("Are you sure you want to delete the selected entries?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					List<Object> items = new();

					foreach (var item in dataGrid_Balance.SelectedItems)
						items.Add(item);

					foreach (var item in items)
					{
						if (item is Override overrideItem)
							Budgeter.BalanceOverrides.Remove(overrideItem);
						else if (item is Charge chargeItem)
							Budgeter.Charges.Remove(chargeItem);
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
