using System;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Budgeter
{
	public class Budget : INotifyPropertyChanged, IJsonOnDeserialized
	{
		[JsonInclude]
		public ObservableCollection<Account> Accounts
		{
			get { return m_Accounts; }
			private set { m_Accounts = value; }
		}

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
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}


		public static Budget? Load(String filename)
		{
			string jsonString = File.ReadAllText(filename);
			var ret = JsonSerializer.Deserialize<Budget>(jsonString);
			return ret;
		}

		public Budget()
		{
			OnDeserialized();
		}

		public void OnDeserialized()
		{
			m_Accounts ??= new();
			m_Accounts = new (m_Accounts.OrderBy(val => val.Name));
			m_Accounts.CollectionChanged += Accounts_CollectionChanged;

			// Get the event handler in place to update the account totals
			Accounts_CollectionChanged(null, new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Add, m_Accounts));
		}

		void Account_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != null)
				NotifyPropertyChanged(e.PropertyName);

			// Propagate the balance change from the account balance to the accounts total
			if (e.PropertyName == nameof(Account.Balance))
			{
				NotifyPropertyChanged(nameof(AccountsTotal));
			}			
		}
		void Accounts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			NotifyPropertyChanged(nameof(AccountsTotal));

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
