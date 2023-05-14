using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Budgeter
{
	public class Account : INotifyPropertyChanged, IJsonOnDeserialized
	{
		[JsonIgnore]
		public double Balance { get { return Today.Balance; } }
		[JsonIgnore]
		public ObservableCollection<AccountEntry> Entries { get { return m_Entries; } }
		[JsonIgnore]
		public Today Today { get { return m_Today; } }


		public String Name
		{
			get { return m_Name; }
			set { if (m_Name == value) return; m_Name = value; NotifyPropertyChanged(); }
		}
		[JsonInclude]
		public ObservableCollection<RecurringChargeTemplate> RecurringChargeTemplates
		{
			get { return m_RecurringChargeTemplates; }
			private set { m_RecurringChargeTemplates = value; }
		}
		[JsonInclude]
		public ObservableCollection<Override> BalanceOverrides
		{
			get { return m_BalanceOverrides; }
			private set { m_BalanceOverrides = value; }
		}
		[JsonInclude]
		public ObservableCollection<Charge> Charges
		{
			get { return m_Charges; }
			private set { m_Charges = value; }
		}
		public int DaysToForecast
		{
			get { return m_DaysToForecast; }
			set { if (m_DaysToForecast == value) return; m_DaysToForecast = value; UpdateEntries(); NotifyPropertyChanged(); }
		}



		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}


		public Account()
		{
			OnDeserialized();
		}
		public void OnDeserialized()
		{
			m_Today ??= new();
			m_RecurringChargeTemplates ??= new();
			m_BalanceOverrides ??= new();
			m_Charges ??= new();

			m_RecurringChargeTemplates = new ObservableCollection<RecurringChargeTemplate>(m_RecurringChargeTemplates.OrderBy(val => val.Name));

			m_RecurringChargeTemplates.CollectionChanged += delegate (object? o, NotifyCollectionChangedEventArgs e)
			{
				UpdateEntries();
				NotifyPropertyChanged(nameof(RecurringChargeTemplates));
			};
			m_BalanceOverrides.CollectionChanged += delegate (object? o, NotifyCollectionChangedEventArgs e)
			{
				UpdateEntries();
				NotifyPropertyChanged(nameof(BalanceOverrides));
			};
			m_Charges.CollectionChanged += delegate (object? o, NotifyCollectionChangedEventArgs e)
			{
				UpdateEntries();
				NotifyPropertyChanged(nameof(Charges));
			};

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
				NotifyPropertyChanged(nameof(Balance));
			NotifyPropertyChanged(nameof(Entries));
		}

		void Entry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Propagate the balance change from the account entry to the account
			if (e.PropertyName == nameof(AccountEntry.Balance) || 
				e.PropertyName == nameof(AccountEntry.Amount) || 
				e.PropertyName == nameof(AccountEntry.Enabled))
			{
				NotifyPropertyChanged(nameof(Balance));
			}

			NotifyPropertyChanged(nameof(Entries));
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
