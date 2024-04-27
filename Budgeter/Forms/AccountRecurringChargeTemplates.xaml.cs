using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Budgeter
{
	/// <summary>
	/// Interaction logic for AccountRecurringChargeTemplates.xaml
	/// </summary>
	public partial class AccountRecurringChargeTemplates : UserControl, IBudgetCommands, INotifyPropertyChanged
	{
		BudgetView? m_BudgetView;
		public BudgetView? BudgetView
		{
			get { return m_BudgetView; }
			set
			{
				if (m_BudgetView != null)
				{
					SelectedAccount = null;
					m_BudgetView.PropertyChanged -= OnBudgetViewPropertyChanged;
				}

				m_BudgetView = value;

				if (m_BudgetView != null)
				{
					m_BudgetView.PropertyChanged += OnBudgetViewPropertyChanged;
					SelectedAccount = m_BudgetView.SelectedAccount;
				}

				NotifyPropertyChanged(nameof(BudgetView));
			}
		}

		Account? m_SelectedAccount;
		Account? SelectedAccount
		{
			get { return m_SelectedAccount; }
			set
			{
				m_SelectedAccount = value;

				dataGrid_Templates.ItemsSource = SelectedAccount?.RecurringChargeTemplates;

				NotifyPropertyChanged(nameof(SelectedAccount));
				OnBudgetViewPropertyChanged(null, new PropertyChangedEventArgs(nameof(BudgetView.SelectedRecurringChargeTemplates)));

				if (m_SelectedAccount != null)
				{
					NumericUpDown_DaysToForecast.Value = m_SelectedAccount.DaysToForecast;
				}
			}
		}


		bool m_dataGrid_Templates_Updating = false;
		private void OnBudgetViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (m_BudgetView == null)
				return;

			if (e.PropertyName == nameof(BudgetView.SelectedAccount))
			{
				SelectedAccount = m_BudgetView.SelectedAccount;
			}
			else if (e.PropertyName == nameof(BudgetView.SelectedRecurringChargeTemplates) && !m_dataGrid_Templates_Updating)
			{
				m_dataGrid_Templates_Updating = true;
				dataGrid_Templates.SelectedItems.Clear();
				var entries = m_BudgetView.SelectedRecurringChargeTemplates;
				if (entries != null)
				{
					foreach (var entry in entries)
						dataGrid_Templates.SelectedItems.Add(entry);
				}
				m_dataGrid_Templates_Updating = false;
			}
		}



		private void DataGrid_Templates_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (m_BudgetView == null || m_dataGrid_Templates_Updating)
				return;

			HashSet<RecurringChargeTemplate> entries = new();

			foreach (var item in dataGrid_Templates.SelectedItems)
			{
				if (item is RecurringChargeTemplate entry)
					entries.Add(entry);
			}

			m_dataGrid_Templates_Updating = true;
			m_BudgetView.SelectedRecurringChargeTemplates = entries;
			m_dataGrid_Templates_Updating = false;
		}


		public AccountRecurringChargeTemplates()
		{
			InitializeComponent();
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}


		private void NumericUpDown_ValueChanged(object sender, NumericUpDownEventArgs e)
		{
			if (m_BudgetView != null && m_BudgetView.SelectedAccount != null)
				m_BudgetView.SelectedAccount.DaysToForecast = (int)e.NewValue;
		}


		void OnCommand_RecurringTransactions_New(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).RecurringTransactions_New(sender, e);
		void OnCommand_RecurringTransactions_Delete(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).RecurringTransactions_Delete(sender, e);
	}
}
