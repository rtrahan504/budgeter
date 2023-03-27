using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Budgeter
{
	public class BudgetView : INotifyPropertyChanged
    {
		Budget m_Budget;
		public Budget Budget
		{
			get { return m_Budget; }
			set
			{
				m_Budget = value;
                NotifyPropertyChanged(nameof(Budget));
            }
        }

        Account? m_SelectedAccount;
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
                NotifyPropertyChanged(nameof(SelectedAccount));
                NotifyPropertyChanged(nameof(SelectedAccountEntries));
            }
        }

        HashSet<AccountEntry> m_SelectedAccountEntries = new();
        public HashSet<AccountEntry> SelectedAccountEntries
        {
            get
            {
                return m_SelectedAccountEntries.ToHashSet();
            }
            set
            {
                m_SelectedAccountEntries = value;
                NotifyPropertyChanged(nameof(SelectedAccountEntries));

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

        HashSet<RecurringChargeTemplate> m_SelectedRecurringChargeTemplates = new();
        public HashSet<RecurringChargeTemplate> SelectedRecurringChargeTemplates
        {
            get
            {
                return m_SelectedRecurringChargeTemplates.ToHashSet();
            }
            set
            {
                m_SelectedRecurringChargeTemplates = value;
                NotifyPropertyChanged(nameof(SelectedRecurringChargeTemplates));

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


        public BudgetView()
		{
			m_Budget = new();
		}

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var e = new PropertyChangedEventArgs(propertyName);
            PropertyChanged?.Invoke(this, e);
        }
    }
}
