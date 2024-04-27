using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Budgeter
{
	/// <summary>
	/// Interaction logic for AccountBalanceSheet.xaml
	/// </summary>
	public partial class AccountBalanceSheet : UserControl, IBudgetCommands, INotifyPropertyChanged
	{
		BudgetView? m_BudgetView;
		Account? m_SelectedAccount;
		bool m_SelectedAccountEntriesUpdating = false;

		public BudgetView? BudgetView
		{
			get { return m_BudgetView; }
			set
			{
				if (m_BudgetView != null)
				{
					m_BudgetView.PropertyChanged -= OnBudgetViewPropertyChanged;

				}

				SelectedAccount = null;
				m_BudgetView = value;

				NotifyPropertyChanged(nameof(BudgetView));

				if (m_BudgetView != null)
				{
					m_BudgetView.PropertyChanged += OnBudgetViewPropertyChanged;
					SelectedAccount = m_BudgetView.SelectedAccount;
				}
			}
		}

		public Account? SelectedAccount
		{
			get { return m_SelectedAccount; }
			set
			{
				if (m_SelectedAccount != null)
				{
					m_SelectedAccount.PropertyChanged -= OnSelectedAccountPropertyChanged;
				}

				m_SelectedAccount = m_BudgetView?.SelectedAccount;

				if (m_SelectedAccount != null)
				{
					m_SelectedAccount.PropertyChanged += OnSelectedAccountPropertyChanged;
				}

				NotifyPropertyChanged(nameof(SelectedAccount));
			}
		}

		private void OnBudgetViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (m_BudgetView == null)
				return;

			if (e.PropertyName == nameof(BudgetView.SelectedAccount))
			{
				SelectedAccount = m_BudgetView?.SelectedAccount;
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


		public void OnCommand_BalanceSheet_Refresh(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_Refresh(sender, e);
		public void OnCommand_BalanceSheet_NewCharge(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_NewCharge(sender, e);
		public void OnCommand_BalanceSheet_NewBalanceOverride(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_NewBalanceOverride(sender, e);
		public void OnCommand_BalanceSheet_Activate(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_Activate(sender, e);
		public void OnCommand_BalanceSheet_Deactivate(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_Deactivate(sender, e);
		public void OnCommand_BalanceSheet_Delete(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_Delete(sender, e);
		public void OnCommand_BalanceSheet_ResetAmount(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_ResetAmount(sender, e);
	}
}
