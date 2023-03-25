using System;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace Budgeter
{
	public class Budget : INotifyPropertyChanged, IJsonOnDeserialized
	{
		public ObservableCollection<Account> Accounts { get { return m_Accounts; } set { m_Accounts = value; } }

		public double AccountsTotal
		{
			get
			{
				double ret = 0;
				foreach (var account in Accounts)
					ret += account.Balance;
				return ret;
			}
		}

		
		public event PropertyChangedEventHandler? PropertyChanged;
		void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
		}

		public static Budget? Load(String filename)
		{
			string jsonString = File.ReadAllText(filename);
			var ret = JsonSerializer.Deserialize<Budget>(jsonString);
			return ret;
		}

		public Budget()
		{
			m_Accounts.CollectionChanged += Accounts_CollectionChanged;
		}

		public void OnDeserialized()
		{
			m_Accounts ??= new();
			m_Accounts = new ObservableCollection<Account>(m_Accounts.OrderBy(val => val.Name));
			m_Accounts.CollectionChanged += Accounts_CollectionChanged;

			Accounts_CollectionChanged(null, new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Add, m_Accounts));
		}

		void Account_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Propagate the balance change from the account entry to the account
			if (e.PropertyName == nameof(Account.Balance))
			{
				OnPropertyChanged(nameof(AccountsTotal));
			}
		}
		void Accounts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			OnPropertyChanged(nameof(AccountsTotal));

			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
			{
				foreach (var item in e.NewItems)
				{
					if (item is Account account)
					{
						account.PropertyChanged += Account_PropertyChanged;
					}
				}
			}
		}

		ObservableCollection<Account> m_Accounts = new();
	}
}
