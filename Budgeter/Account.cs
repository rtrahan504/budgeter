using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Budgeter
{
	public class Account : INotifyPropertyChanged, IJsonOnDeserialized
	{
		public double Balance { get { return Today.Balance; } }
		[JsonIgnore]
		public ObservableCollection<AccountEntry> Entries { get { return m_Entries; } }
		public Today Today { get { return m_Today; } }


		public String Name
		{
			get { return m_Name; }
			set { if (m_Name == value) return; m_Name = value; OnPropertyChanged(nameof(Name)); }
		}
		public ObservableCollection<RecurringChargeTemplate> RecurringChargeTemplates
		{
			get { return m_RecurringChargeTemplates; }
			set { m_RecurringChargeTemplates = value; UpdateEntries(); OnPropertyChanged(nameof(RecurringChargeTemplates)); }
		}
		public ObservableCollection<Override> BalanceOverrides
		{
			get { return m_BalanceOverrides; }
			set { m_BalanceOverrides = value; UpdateEntries(); OnPropertyChanged(nameof(BalanceOverrides)); }
		}
		public ObservableCollection<Charge> Charges
		{
			get { return m_Charges; }
			set { m_Charges = value; UpdateEntries(); OnPropertyChanged(nameof(Charges)); }
		}
		public int DaysToForecast
		{
			get { return m_DaysToForecast; }
			set { if (m_DaysToForecast == value) return; m_DaysToForecast = value; UpdateEntries(); OnPropertyChanged(nameof(DaysToForecast)); }
		}



		public event PropertyChangedEventHandler? PropertyChanged;
		void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}


		public Account()
		{
			m_RecurringChargeTemplates.CollectionChanged += OnCollectionChanged;
			m_BalanceOverrides.CollectionChanged += OnCollectionChanged;
			m_Charges.CollectionChanged += OnCollectionChanged;
		}
		public void OnDeserialized()
		{
			m_Today ??= new();
			m_RecurringChargeTemplates ??= new();
			m_BalanceOverrides ??= new();
			m_Charges ??= new();

			m_RecurringChargeTemplates = new ObservableCollection<RecurringChargeTemplate>(m_RecurringChargeTemplates.OrderBy(val => val.Name));

			m_RecurringChargeTemplates.CollectionChanged += OnCollectionChanged;
			m_BalanceOverrides.CollectionChanged += OnCollectionChanged;
			m_Charges.CollectionChanged += OnCollectionChanged;

			UpdateEntries();
		}

		public void OnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			UpdateEntries();
		}

		public void UpdateEntries()
		{
			m_Today ??= new();

			var oldBalance = Balance;

			List<AccountEntry> tmpList = new()
			{
				m_Today
			};

			foreach (var item in BalanceOverrides)
				tmpList.Add(item);
			foreach (var item in Charges)
				tmpList.Add(item);
			foreach (RecurringChargeTemplate chargeTemplate in RecurringChargeTemplates)
				tmpList.AddRange(chargeTemplate.GetRecurringCharges(m_DaysToForecast));

			tmpList.Sort((lhs, rhs) =>
			{
				if (rhs.Date != lhs.Date)
					return rhs.Date.CompareTo(lhs.Date);
				else if (rhs.Amount.HasValue && lhs.Amount.HasValue)
					return lhs.Amount.Value.CompareTo(rhs.Amount.Value);
				else
					return 0;
			});

			m_Entries.Clear();
			AccountEntry? previous = null;
			for (int i = tmpList.Count - 1; i >= 0; --i)
			{
				var entry = tmpList[i];
				if (entry == null) continue;
				entry.Predecessor = previous;
				entry.PropertyChanged -= Entry_PropertyChanged;
                entry.PropertyChanged += Entry_PropertyChanged;
				previous = entry;
				m_Entries.Insert(0, entry);
			}

			// Rebuilding the account may have changed the balance
			if (oldBalance != Balance)
            {
                OnPropertyChanged(nameof(Balance));
            }

            OnPropertyChanged(nameof(Entries));
        }

		void Entry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Propagate the balance change from the account entry to the account
			if (e.PropertyName == nameof(AccountEntry.Balance) || 
				e.PropertyName == nameof(AccountEntry.Amount) || 
				e.PropertyName == nameof(AccountEntry.Enabled))
			{
				OnPropertyChanged(nameof(Balance));
			}

			OnPropertyChanged(nameof(Entries));
		}

		Today m_Today = new();
		String m_Name = "{new account}";
		ObservableCollection<RecurringChargeTemplate> m_RecurringChargeTemplates = new();
		ObservableCollection<Override> m_BalanceOverrides = new();
		ObservableCollection<Charge> m_Charges = new();
		readonly ObservableCollection<AccountEntry> m_Entries = new();
		int m_DaysToForecast = 62;
	}
}
