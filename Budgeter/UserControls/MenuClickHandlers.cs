using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Budgeter
{
	class MenuClickHandlers
	{
		public static void OnMenuClick(string menuItemTag, BudgetView? budgetView)
		{
			if (budgetView == null)
				return;

			if (menuItemTag == "Account_NewAccount")
			{
				budgetView.Budget.Accounts.Add(new Account());
			}
			else if (menuItemTag == "Account_DeleteAccount")
			{
				if (budgetView.SelectedAccount != null && System.Windows.MessageBox.Show("Are you sure you want to delete the selected account?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					budgetView.Budget.Accounts.Remove(budgetView.SelectedAccount);
				}
			}
			else if (menuItemTag == "BalanceSheet_Refresh")
			{
				budgetView.SelectedAccount?.UpdateEntries();
			}
			else if (menuItemTag == "BalanceSheet_Activate")
			{
				foreach (var entry in budgetView.SelectedAccountEntries)
					entry.Enabled = true;
			}
			else if (menuItemTag == "BalanceSheet_Deactivate")
			{
				foreach (var entry in budgetView.SelectedAccountEntries)
					entry.Enabled = false;
			}
			else if (menuItemTag == "BalanceSheet_NewBalanceOverride" || menuItemTag == "BalanceSheet_NewCharge")
			{
				if (budgetView.SelectedAccount == null)
					return;

				DateTime date = budgetView.SelectedAccountEntries.Count > 0 ? budgetView.SelectedAccountEntries.First().Date : DateTime.Now;

				if (menuItemTag == "BalanceSheet_NewCharge")
				{
					var newVal = new Charge() { Date = date };
					budgetView.SelectedAccount.Charges.Add(newVal);
					budgetView.SelectedAccountEntries = new HashSet<AccountEntry> { newVal };
				}
				if (menuItemTag == "BalanceSheet_NewBalanceOverride")
				{
					var newVal = new Override() { Date = date };
					budgetView.SelectedAccount.BalanceOverrides.Add(newVal);
					budgetView.SelectedAccountEntries = new HashSet<AccountEntry> { newVal };
				}
			}
			else if (menuItemTag == "BalanceSheet_Delete")
			{
				if (budgetView.SelectedAccount == null)
					return;

				List<Object> items = new();
				HashSet<string> messages = new();
				foreach (var entry in budgetView.SelectedAccountEntries)
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
							budgetView.SelectedAccount.BalanceOverrides.Remove(overrideItem);
						else if (item is Charge chargeItem)
							budgetView.SelectedAccount.Charges.Remove(chargeItem);
						else if (item is RecurringCharge recurring)
							budgetView.SelectedAccount.Entries.Remove(recurring);
					}
				}
			}
			else if (menuItemTag == "BalanceSheet_ResetAmount")
			{
				foreach (var entry in budgetView.SelectedAccountEntries)
				{
					if (entry is RecurringCharge charge)
						charge.ResetAmount();
					else
						entry.Amount = null;
				}
			}
			else if (menuItemTag == "RecurringTransactions_New")
			{
				RecurringChargeTemplate val = new()
				{
					Name = "{new recurring charge}"
				};
				budgetView.SelectedAccount?.RecurringChargeTemplates.Add(val);
				budgetView.SelectedRecurringChargeTemplates = new HashSet<RecurringChargeTemplate>() { val };
			}
			else if (menuItemTag == "RecurringTransactions_Delete")
			{
				HashSet<string> messages = new();
				var items = budgetView.SelectedRecurringChargeTemplates;
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

				if (budgetView.SelectedAccount != null && System.Windows.MessageBox.Show("Are you sure you want to delete the selected entries?\n\n" + message, "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					foreach (RecurringChargeTemplate item in items)
					{
						budgetView.SelectedAccount.RecurringChargeTemplates.Remove(item);
					}
				}
			}
		}
	}
}
