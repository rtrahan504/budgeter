using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.ObjectModel;

namespace Budgeter
{
	public enum RecurrenceIntervals
	{
		None,
		Days,
		Months
	}
	public enum AmountModes
	{
		Predefined,
		Variable
	}


	[Serializable]
	public abstract class BudgetEntry
	{
		public bool Enabled { get; set; }
		public virtual String Name { get; }
		public abstract String Type { get; }
		public virtual DateTime Date { get; protected set; }
		public virtual double? Amount { get; set; }
		public virtual double Balance
		{
			get
			{
				if (Predecessor == null)
					return Enabled ? (Amount ?? 0) : 0;
				else
					return Predecessor.Balance + (Enabled ? (Amount ?? 0) : 0);
			}
		}


		public BudgetEntry? Predecessor { get; set; }

		protected BudgetEntry()
		{
			Enabled = true;
			Name = "";
			Predecessor = null;
		}
	}

	[Serializable]
	public class Today : BudgetEntry
	{
		public override String Name { get { return ""; } }
		public override String Type { get { return "Today"; } }
		public override DateTime Date { get { return DateTime.Now; } }
	}

	[Serializable]
	public class Override : BudgetEntry
	{
		public new String Name
		{
			get { return m_Name; }
			set { m_Name = value; }
		}
		public override String Type { get { return "Balance Override"; } }

		public new DateTime Date
		{
			get => base.Date;
			set => base.Date = value;
		}
		public override double Balance
		{
			get
			{
				if (Enabled)
				{
					return Amount.GetValueOrDefault(0.0);
				}
				else
				{
					return base.Balance;
				}
			}
		}

		public Override()
		{
			Date = DateTime.Now;
		}

		String m_Name = "{new override}";
	}

	[Serializable]
	public class Charge : BudgetEntry
	{
		public new String Name
		{
			get { return m_Name; }
			set { m_Name = value; }
		}
		public override String Type { get { return "Charge"; } }
		public new DateTime Date
		{
			get => base.Date;
			set => base.Date = value;
		}

		public Charge()
		{
			Date = DateTime.Now;
		}

		String m_Name = "{new charge}";
	}

	[Serializable]
	public class RecurringChargeTemplate
	{
		public String Name { get; set; }
		public DateTime Date { get; set; }

		public RecurrenceIntervals RecurrenceInterval { get; set; }

		public UInt32 Interval { get; set; }

		public AmountModes AmountMode { get; set; }
		public double PredefinedAmount { get; set; }

		public List<RecurringCharge> GetRecurringCharges(int daysToForecast)
		{
			if (RecurrenceInterval == RecurrenceIntervals.None || Interval == 0 || Date.Year < 2000)
			{
				if (m_RecurringCharges.Count != 1)
				{
					m_RecurringCharges.Clear();
					m_RecurringCharges.Add(new RecurringCharge(this, 0));
				}
			}
			else
			{
				var endDate = DateTime.Now.AddDays(daysToForecast);

				var oldCharges = m_RecurringCharges.ToList();

				m_RecurringCharges.Clear();
				for (UInt32 index = 0; true; ++index)
				{
					var newCharge = new RecurringCharge(this, index);
					var reuseCharge = oldCharges.Find((val) => DateOnly.FromDateTime(val.Date) == DateOnly.FromDateTime(newCharge.Date));
					var charge = reuseCharge ?? newCharge;

					if (charge.Date < endDate)
						m_RecurringCharges.Add(charge);
					else
						break;
				}
			}

			return m_RecurringCharges;
		}

		public RecurringChargeTemplate()
		{
			Name = "";
			Date = DateTime.Now;
			AmountMode = AmountModes.Predefined;
			PredefinedAmount = 0;
			RecurrenceInterval = RecurrenceIntervals.None;
			Interval = 0;
		}
		public RecurringChargeTemplate(String name, DateTime date, AmountModes amountMode, double predefinedAmount)
		{
			Name = name;
			Date = date;
			AmountMode = amountMode;
			PredefinedAmount = predefinedAmount;
			RecurrenceInterval = RecurrenceIntervals.None;
			Interval = 0;
		}
		public RecurringChargeTemplate(String name, DateTime date, AmountModes amountMode, double predefinedAmount, RecurrenceIntervals recurrenceInterval, UInt32 interval)
		{
			Name = name;
			Date = date;
			AmountMode = amountMode;
			PredefinedAmount = predefinedAmount;
			RecurrenceInterval = recurrenceInterval;
			Interval = interval;
		}

		private List<RecurringCharge> m_RecurringCharges = new();
	}
	[Serializable]
	public class RecurringCharge : BudgetEntry
	{
		public RecurringChargeTemplate Template { get; private set; }
		public UInt32 Index { get; private set; }
		public override String Name { get { return Template.Name; } }
		public override String Type { get { return "Recurring"; } }
		public override DateTime Date
		{
			get
			{
				if (Template.RecurrenceInterval == RecurrenceIntervals.Days)
					return Template.Date.AddDays((int)(Index * Template.Interval));
				else if (Template.RecurrenceInterval == RecurrenceIntervals.Months)
					return Template.Date.AddMonths((int)(Index * Template.Interval));
				else
					return Template.Date;
			}
		}
		public override double? Amount
		{
			get
			{
				if (!Enabled)
					return null;
				else if (Template.AmountMode == AmountModes.Predefined)
					return Template.PredefinedAmount;
				else
					return definedAmount;
			}
			set
			{
				if (Template.AmountMode == AmountModes.Predefined && value.HasValue)
					Template.PredefinedAmount = value.GetValueOrDefault(0);
				else
					definedAmount = value == 0 ? null : value;
			}
		}



		public RecurringCharge()
		{
			Template = new RecurringChargeTemplate();
			Index = 0;
		}
		public RecurringCharge(RecurringChargeTemplate template, UInt32 index)
		{
			Template = template;
			Index = index;
		}

		double? definedAmount = null;
	}


	[Serializable]
	public class Budget
	{

		public ObservableCollection<Account> Accounts { get; private set; }


		public void Save(String filename)
		{
			using (var stream = new FileStream(filename, FileMode.OpenOrCreate))
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(stream, this);
			};
		}

		public static Budget? Load(String filename)
		{
			Budget? tmpBudget = null;

			try
			{
				using (var stream = new FileStream(filename, FileMode.OpenOrCreate))
				{
					var formatter = new BinaryFormatter();
					tmpBudget = formatter.Deserialize(stream) as Budget;
					if (tmpBudget != null)
					{
						foreach (var account in tmpBudget.Accounts)
						{
							account.RecurringChargeTemplates.CollectionChanged += account.OnCollectionChanged;
							account.BalanceOverrides.CollectionChanged += account.OnCollectionChanged;
							account.Charges.CollectionChanged += account.OnCollectionChanged;
						}
					}
				};
			}
			catch
			{
				return null;
			}


			return tmpBudget;
		}

		public Budget()
		{
			Accounts = new ObservableCollection<Account>();
		}
	}


	[Serializable]
	public class Account
	{
		public Today Today { get; private set; }
		public String Name { get; set; }
		public ObservableCollection<RecurringChargeTemplate> RecurringChargeTemplates { get; set; }
		public ObservableCollection<Override> BalanceOverrides { get; set; }
		public ObservableCollection<Charge> Charges { get; set; }
		public ObservableCollection<BudgetEntry> Entries { get; set; }
		public int DaysToForecast
		{
			get { return m_DaysToForecast; }
			set { m_DaysToForecast = value; UpdateEntries(); }
		}


		public Account()
		{
			Today = new Today();
			Name = "{new account}";
			RecurringChargeTemplates = new ObservableCollection<RecurringChargeTemplate>();
			BalanceOverrides = new ObservableCollection<Override>();
			Charges = new ObservableCollection<Charge>();
			Entries = new ObservableCollection<BudgetEntry>();

			RecurringChargeTemplates.CollectionChanged += OnCollectionChanged;
			BalanceOverrides.CollectionChanged += OnCollectionChanged;
			Charges.CollectionChanged += OnCollectionChanged;
		}

		public void OnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			UpdateEntries();
		}

		public void UpdateEntries()
		{
			if (Today == null)
				Today = new();

			List<BudgetEntry> tmpList = new()
			{
				Today
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

			Entries.Clear();
			BudgetEntry? previous = null;
			for (int i = tmpList.Count; i > 0; --i)
			{
				var entry = tmpList[i - 1];
				entry.Predecessor = previous;
				previous = entry;

				Entries.Insert(0, entry);
			}
		}

		int m_DaysToForecast = 62;
	}
}
