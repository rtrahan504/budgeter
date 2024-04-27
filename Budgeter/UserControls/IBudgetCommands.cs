using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace Budgeter
{
	public interface IBudgetCommands
	{
		protected BudgetView? BudgetView
		{
			get;
		}

		public static RoutedCommand Command_Account_NewAccount = new RoutedCommand();
		public void Account_NewAccount(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null)
				return;

			BudgetView.Budget.Accounts.Add(new Account());
		}
		public static RoutedCommand Command_Account_DeleteAccount = new RoutedCommand();
		public void Account_DeleteAccount(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null)
				return;

			if (BudgetView.SelectedAccount != null && System.Windows.MessageBox.Show("Are you sure you want to delete the selected account?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				BudgetView.Budget.Accounts.Remove(BudgetView.SelectedAccount);
			}
		}


		public static RoutedCommand Command_BalanceSheet_Refresh = new RoutedCommand();
		public void BalanceSheet_Refresh(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null)
				return;

			BudgetView.SelectedAccount?.UpdateEntries();
		}
		public static RoutedCommand Command_BalanceSheet_NewCharge = new RoutedCommand();
		public void BalanceSheet_NewCharge(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null || BudgetView.SelectedAccount == null)
				return;

			DateTime date = BudgetView.SelectedAccountEntries.Count > 0 ? BudgetView.SelectedAccountEntries.First().Date : DateTime.Now;

			var newVal = new Charge() { Date = date };
			BudgetView.SelectedAccount.Charges.Add(newVal);
			BudgetView.SelectedAccountEntries = new HashSet<AccountEntry> { newVal };
		}
		public static RoutedCommand Command_BalanceSheet_NewBalanceOverride = new RoutedCommand();
		public void BalanceSheet_NewBalanceOverride(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null || BudgetView.SelectedAccount == null)
				return;

			DateTime date = BudgetView.SelectedAccountEntries.Count > 0 ? BudgetView.SelectedAccountEntries.First().Date : DateTime.Now;

			var newVal = new Override() { Date = date };
			BudgetView.SelectedAccount.BalanceOverrides.Add(newVal);
			BudgetView.SelectedAccountEntries = new HashSet<AccountEntry> { newVal };
		}
		public static RoutedCommand Command_BalanceSheet_Activate = new RoutedCommand();
		public void BalanceSheet_Activate(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null)
				return;

			foreach (var entry in BudgetView.SelectedAccountEntries)
				entry.Enabled = true;
		}
		public static RoutedCommand Command_BalanceSheet_Deactivate = new RoutedCommand();
		public void BalanceSheet_Deactivate(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null)
				return;

			foreach (var entry in BudgetView.SelectedAccountEntries)
				entry.Enabled = false;
		}
		public static RoutedCommand Command_BalanceSheet_Delete = new RoutedCommand();
		public void BalanceSheet_Delete(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null || BudgetView.SelectedAccount == null)
				return;

			List<Object> items = new();
			HashSet<string> messages = new();
			foreach (var entry in BudgetView.SelectedAccountEntries)
			{
				if (entry is Today)
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

			if (items.Count > 0 && System.Windows.MessageBox.Show("Are you sure you want to delete the selected entries?\n\n" + message, "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				foreach (var item in items)
				{
					if (item is Override overrideItem)
						BudgetView.SelectedAccount.BalanceOverrides.Remove(overrideItem);
					else if (item is Charge chargeItem)
						BudgetView.SelectedAccount.Charges.Remove(chargeItem);
					else if (item is RecurringCharge recurring)
						BudgetView.SelectedAccount.Entries.Remove(recurring);
				}
			}
		}
		public static RoutedCommand Command_BalanceSheet_ResetAmount = new RoutedCommand();
		public void BalanceSheet_ResetAmount(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null)
				return;

			foreach (var entry in BudgetView.SelectedAccountEntries)
			{
				if (entry is RecurringCharge charge)
					charge.ResetAmount();
				else
					entry.Amount = null;
			}
		}


		public static RoutedCommand Command_RecurringTransactions_New = new RoutedCommand();
		public void RecurringTransactions_New(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null)
				return;

			RecurringChargeTemplate val = new()
			{
				Name = "{new recurring charge}"
			};
			BudgetView.SelectedAccount?.RecurringChargeTemplates.Add(val);
			BudgetView.SelectedRecurringChargeTemplates = new HashSet<RecurringChargeTemplate>() { val };
		}
		public static RoutedCommand Command_RecurringTransactions_Delete = new RoutedCommand();
		public void RecurringTransactions_Delete(object sender, ExecutedRoutedEventArgs e)
		{
			if (BudgetView == null)
				return;

			HashSet<string> messages = new();
			var items = BudgetView.SelectedRecurringChargeTemplates;
			if (items == null)
				return;

			foreach (var item in items)
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

			if (BudgetView.SelectedAccount != null && System.Windows.MessageBox.Show("Are you sure you want to delete the selected entries?\n\n" + message, "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				foreach (RecurringChargeTemplate item in items)
				{
					BudgetView.SelectedAccount.RecurringChargeTemplates.Remove(item);
				}
			}
		}
	}
}
