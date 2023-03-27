using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Budgeter
{
	/// <summary>
	/// Interaction logic for AccountList.xaml
	/// </summary>
	public partial class AccountList : UserControl, INotifyPropertyChanged
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


		private void OnBudgetViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (m_BudgetView == null)
				return;

			if (e.PropertyName == nameof(BudgetView.SelectedAccount))
			{
				if (dataGrid_Accounts.SelectedItem != m_BudgetView.SelectedAccount)
					dataGrid_Accounts.SelectedItem = m_BudgetView.SelectedAccount;
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

		private void OnMenuClick(object sender, RoutedEventArgs e)
		{
			MenuItem? menuItem = sender as MenuItem;

			if (menuItem == null || m_BudgetView == null || menuItem.Tag is not string menuItemTag)
				return;

			MenuClickHandlers.OnMenuClick(menuItemTag, m_BudgetView);
		}
	}
}
