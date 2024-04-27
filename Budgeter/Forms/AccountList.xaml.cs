using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Budgeter
{
	/// <summary>
	/// Interaction logic for AccountList.xaml
	/// </summary>
	public partial class AccountList : UserControl, IBudgetCommands, INotifyPropertyChanged
	{
		BudgetView? m_BudgetView;
		public BudgetView? BudgetView
		{
			get { return m_BudgetView; }
			set
			{
				if (m_BudgetView != null)
				{
					m_BudgetView.Budget.PropertyChanged -= OnBudgetViewPropertyChanged;
					m_BudgetView.PropertyChanged -= OnBudgetViewPropertyChanged;
				}

				m_BudgetView = value;

				if (m_BudgetView != null)
				{
					m_BudgetView.PropertyChanged += OnBudgetViewPropertyChanged;
					m_BudgetView.Budget.PropertyChanged += OnBudgetViewPropertyChanged;
				}

				NotifyPropertyChanged(nameof(BudgetView));
				NotifyPropertyChanged(nameof(CurrentAccounts));

				OnBudgetViewPropertyChanged(null, new PropertyChangedEventArgs(nameof(BudgetView.SelectedAccount)));
			}
		}

		public ObservableCollection<Account>? CurrentAccounts
		{
			get
			{
				return m_BudgetView?.Budget.Accounts;
			}
		}


		private void OnBudgetViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (m_BudgetView == null)
				return;

			if (e.PropertyName == nameof(BudgetView.SelectedAccount))
			{
				if (dataGrid_Accounts.SelectedItem != m_BudgetView.SelectedAccount)
					dataGrid_Accounts.SelectedItem = m_BudgetView.SelectedAccount;
			}
			if (e.PropertyName == nameof(Budget.Accounts))
			{
				NotifyPropertyChanged(nameof(CurrentAccounts));
			}
		}

		public AccountList()
		{
			InitializeComponent();
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			var e = new PropertyChangedEventArgs(propertyName);
			PropertyChanged?.Invoke(this, e);
		}

		private void DataGrid_Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (m_BudgetView == null)
				return;

			if (m_BudgetView.SelectedAccount != (e.AddedItems.Count == 1 ? e.AddedItems[0] as Account : null))
				m_BudgetView.SelectedAccount = e.AddedItems.Count == 1 ? e.AddedItems[0] as Account : null;
		}
		
		public void OnCommand_Account_NewAccount(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).Account_NewAccount(sender, e);
		public void OnCommand_Account_DeleteAccount(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).Account_DeleteAccount(sender, e);

	}
}
