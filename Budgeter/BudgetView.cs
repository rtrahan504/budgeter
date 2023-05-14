using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Budgeter
{
	public class BudgetView : INotifyPropertyChanged, IJsonOnDeserialized
	{
		Budget m_Budget;
		Account? m_SelectedAccount;
		HashSet<AccountEntry> m_SelectedAccountEntries = new();
		HashSet<RecurringChargeTemplate> m_SelectedRecurringChargeTemplates = new();
		bool m_IsBudgetModified = false;
		String m_CurrentFile = "";

		public String CurrentFile
		{
			get { return m_CurrentFile; }
			set
			{
				m_CurrentFile = value;
				NotifyPropertyChanged(nameof(CurrentFile));
			}
		}

		[JsonInclude]
		public Budget Budget
		{
			get { return m_Budget; }
			private set { m_Budget = value; }
		}

		[JsonIgnore]
		public Account? SelectedAccount
		{
			get
			{
				return m_SelectedAccount;
			}
			set
			{
				m_SelectedAccount = value;
				SelectedAccountEntries = new();
				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(SelectedAccountEntries));
			}
		}

		[JsonIgnore]
		public HashSet<AccountEntry> SelectedAccountEntries
		{
			get
			{
				return m_SelectedAccountEntries;
			}
			set
			{
				m_SelectedAccountEntries = value;
				NotifyPropertyChanged();

				HashSet<RecurringChargeTemplate> templates = new();
				foreach (var entry in m_SelectedAccountEntries)
				{
					if (entry is RecurringCharge templatedCharge)
						templates.Add(templatedCharge.Template);
				}
				m_SelectedRecurringChargeTemplates = templates;
				NotifyPropertyChanged(nameof(SelectedRecurringChargeTemplates));
			}
		}

		[JsonIgnore]
		public HashSet<RecurringChargeTemplate> SelectedRecurringChargeTemplates
		{
			get
			{
				return m_SelectedRecurringChargeTemplates;
			}
			set
			{
				m_SelectedRecurringChargeTemplates = value;
				NotifyPropertyChanged();

				HashSet<AccountEntry> entries = new();
				if (m_SelectedAccount != null)
				{                    
					foreach (var entry in m_SelectedAccount.Entries)
					{
						if (entry is RecurringCharge recurringCharge && m_SelectedRecurringChargeTemplates.Contains(recurringCharge.Template))
							entries.Add(entry);
					}
				}
				m_SelectedAccountEntries = entries;
				NotifyPropertyChanged(nameof(SelectedAccountEntries));
			}
		}

		[JsonIgnore]
		public bool IsBudgetModified
		{
			get { return m_IsBudgetModified; }
			set
			{
				m_IsBudgetModified = value;
				NotifyPropertyChanged();
			}
		}


		public int SelectedAccountIndex
		{
			get
			{
				if (SelectedAccount != null)
					return Budget.Accounts.IndexOf(SelectedAccount);
				else
					return -1;
			}
			set
			{
				if (value < 0 || value >= Budget.Accounts.Count)
					SelectedAccount = null;
				else
					SelectedAccount = this.Budget.Accounts[value];
			}
		}
		public HashSet<int> SelectedAccountEntryIndices
		{
			get
			{
				HashSet<int> ret = new();
				if (SelectedAccount != null)
				{
					foreach (var entry in m_SelectedAccountEntries)
					{
						ret.Add(SelectedAccount.Entries.IndexOf(entry));
					}
				}
				return ret;
			}
			set
			{
				HashSet<AccountEntry> selectedEntries = new();
				if (SelectedAccount != null)
				{
					foreach (var i in value)
					{
						if (i < SelectedAccount.Entries.Count)
							selectedEntries.Add(SelectedAccount.Entries[i]);

					}
				}
				SelectedAccountEntries = selectedEntries;
			}
		}
		public HashSet<int> SelectedRecurringChargeTemplateIndices
		{
			get
			{
				HashSet<int> ret = new();
				if (SelectedAccount != null)
				{
					foreach (var entry in m_SelectedRecurringChargeTemplates)
					{
						ret.Add(SelectedAccount.RecurringChargeTemplates.IndexOf(entry));
					}
				}
				return ret;
			}
			set
			{
				HashSet<RecurringChargeTemplate> selectedEntries = new();
				if (SelectedAccount != null)
				{
					foreach (var i in value)
					{
						if (i < SelectedAccount.RecurringChargeTemplates.Count)
							selectedEntries.Add(SelectedAccount.RecurringChargeTemplates[i]);

					}
				}
				SelectedRecurringChargeTemplates = selectedEntries;
			}
		}

		public BudgetView()
		{
			m_Budget = new();
			m_Budget.PropertyChanged += M_Budget_PropertyChanged;
		}
		public BudgetView(Budget Budget)
		{
			m_Budget = Budget;
			m_Budget.PropertyChanged += M_Budget_PropertyChanged;
		}

		public void OnDeserialized()
		{
			m_Budget.PropertyChanged += M_Budget_PropertyChanged;
			m_IsBudgetModified = false;
		}

		private void M_Budget_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(IsBudgetModified))
				IsBudgetModified = true;
		}

		public void Save(String filename)
		{
			JsonSerializerOptions options = new()
			{
				IgnoreReadOnlyFields = true,
				IgnoreReadOnlyProperties = true,
				WriteIndented = true
			};
			string jsonString = JsonSerializer.Serialize(this, options);
			File.WriteAllText(filename, jsonString);

			IsBudgetModified = false;
			CurrentFile = filename;
		}

		public static BudgetView Load(String filename)
		{
			string jsonString = File.ReadAllText(filename);
			var ret = JsonSerializer.Deserialize<BudgetView>(jsonString);
			if (ret == null)
				throw new Exception("An error occurred loading the budget.");
			ret.CurrentFile = filename;
			return ret;
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
