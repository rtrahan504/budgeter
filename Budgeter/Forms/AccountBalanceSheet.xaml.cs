using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Budgeter
{
	/// <summary>
	/// Interaction logic for AccountBalanceSheet.xaml
	/// </summary>
	public partial class AccountBalanceSheet : UserControl, INotifyPropertyChanged
	{
		BudgetView? m_BudgetView;
		public BudgetView? CurrentBudgetView
		{
			get { return m_BudgetView; }
			set
			{
				if (m_BudgetView != null)
				{
					m_BudgetView.PropertyChanged -= OnBudgetViewPropertyChanged;
				}

				m_BudgetView = value;

				if (m_BudgetView != null)
				{
					m_BudgetView.PropertyChanged += OnBudgetViewPropertyChanged;
				}

				NotifyPropertyChanged(nameof(CurrentBudgetView));
			}
		}

		Account? m_SelectedAccount;
		bool m_SelectedAccountEntriesUpdating = false;

		private void OnBudgetViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (m_BudgetView == null)
				return;

			if (e.PropertyName == nameof(BudgetView.SelectedAccount))
			{
				if (m_SelectedAccount != null)
					m_SelectedAccount.PropertyChanged -= OnSelectedAccountPropertyChanged;

				m_SelectedAccount = m_BudgetView.SelectedAccount;
				NotifyPropertyChanged(nameof(CurrentBudgetView));
				NotifyPropertyChanged(nameof(BudgetView.SelectedAccount));

				if (m_SelectedAccount != null)
					m_SelectedAccount.PropertyChanged += OnSelectedAccountPropertyChanged;
			}
			else if (e.PropertyName == nameof(BudgetView.SelectedAccountEntries) && !m_SelectedAccountEntriesUpdating)
			{
				m_SelectedAccountEntriesUpdating = true;
				dataGrid_Balance.SelectedItems.Clear();
				if (m_BudgetView.SelectedAccountEntries != null)
				{
					foreach (var entry in m_BudgetView.SelectedAccountEntries)
						dataGrid_Balance.SelectedItems.Add(entry);
				}
				m_SelectedAccountEntriesUpdating = false;
			}
		}

		private void OnSelectedAccountPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			Application.Current.Dispatcher.BeginInvoke(
			  DispatcherPriority.Background,
			  () =>
			  {
				  try
				  {
					  if (e.PropertyName == "Balance" || e.PropertyName == "Entries")
					  {
						  dataGrid_Balance.Items.Refresh();
					  }
				  }
				  catch
				  { }
			  });
		}

		public AccountBalanceSheet()
		{
			InitializeComponent();
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			var e = new PropertyChangedEventArgs(propertyName);
			PropertyChanged?.Invoke(this, e);
		}


		private void DataGrid_Balance_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (m_BudgetView == null || m_SelectedAccountEntriesUpdating)
				return;

			HashSet<AccountEntry> entries = new();

			foreach (var item in dataGrid_Balance.SelectedItems)
			{
				if (item is AccountEntry entry)
					entries.Add(entry);
			}

			m_SelectedAccountEntriesUpdating = true;
			m_BudgetView.SelectedAccountEntries = entries;
			m_SelectedAccountEntriesUpdating = false;
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
 
		private void OnMenuClick(object sender, RoutedEventArgs e)
		{
			MenuItem? menuItem = sender as MenuItem;

			if (menuItem == null || m_BudgetView == null || menuItem.Tag is not string menuItemTag)
				return;

			MenuClickHandlers.OnMenuClick(menuItemTag, m_BudgetView);
		}
	}
}
