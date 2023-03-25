using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Budgeter
{
	public enum RecurrenceIntervals
	{
		None,
		Days,
		Months
	}


	public abstract class BudgetEntry : INotifyPropertyChanged
	{
		public bool Enabled
		{
			get { return m_Enabled; }
			set { if (m_Enabled == value) return; m_Enabled = value; OnPropertyChanged(nameof(Enabled)); }
		}
		public virtual String Name
		{
			get { return m_Name; }
			protected set { if (m_Name == value) return; m_Name = value; OnPropertyChanged(nameof(Name)); }
		}
		public abstract String Type { get; }
		public virtual DateTime Date
		{
			get { return m_Date; }
			protected set { if (m_Date == value) return; m_Date = value; OnPropertyChanged(nameof(Date)); }
		}
		public virtual double? Amount
		{
			get { return m_Amount; }
			set { if (m_Amount == value) return; m_Amount = value; OnPropertyChanged(nameof(Amount)); }
		}
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

		internal BudgetEntry? Predecessor
		{
			get { return m_Predecessor; }
			set { m_Predecessor = value; }
		}

		protected BudgetEntry() { }

		bool m_Enabled = true;
		String m_Name = "";
		DateTime m_Date;
		double? m_Amount = null;
		[field: NonSerialized]
		BudgetEntry? m_Predecessor = null;


		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

			if (propertyName == nameof(BudgetEntry.Enabled) ||
				propertyName == nameof(BudgetEntry.Date) ||
				propertyName == nameof(BudgetEntry.Amount))
			{
				OnPropertyChanged(nameof(Balance));
			}
		}
	}

	public class Today : BudgetEntry
	{
		public override String Type { get { return "Today"; } }
		public override DateTime Date { get { return DateTime.Now; } }
	}

	public class Override : BudgetEntry
	{
		public new String Name
		{
			get => base.Name;
			set => base.Name = value;
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
			Name = "{new override}";
		}
	}

	public class Charge : BudgetEntry
	{
		public new String Name
		{
			get => base.Name;
			set => base.Name = value;
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
	}

	public class RecurringChargeTemplate : INotifyPropertyChanged, IJsonOnDeserialized
	{
		public String Name
		{
			get { return m_Name; }
			set { m_Name = value; OnPropertyChanged(nameof(Name)); }
		}
		public DateTime Date
		{
			get { return m_Date; }
			set { m_Date = value; OnPropertyChanged(nameof(Date)); }
		}
		public RecurrenceIntervals RecurrenceInterval
		{
			get { return m_RecurrenceInterval; }
			set { m_RecurrenceInterval = value; OnPropertyChanged(nameof(RecurrenceInterval)); }
		}
		public UInt32 Interval
		{
			get { return m_Interval; }
			set { m_Interval = value; OnPropertyChanged(nameof(Interval)); }
		}
		public double PredefinedAmount
		{
			get { return m_PredefinedAmount; }
			set { m_PredefinedAmount = value; OnPropertyChanged(nameof(PredefinedAmount)); }
		}

		public List<RecurringCharge> RecurringCharges
		{
			get { return m_RecurringCharges; }
			set { m_RecurringCharges = value; OnPropertyChanged(nameof(RecurringCharges)); }
		}

		public List<RecurringCharge> GetRecurringCharges(int daysToForecast)
		{
			bool changed = false;

			if (RecurrenceInterval == RecurrenceIntervals.None || Interval == 0 || Date.Year < 2000)
			{
				if (m_RecurringCharges.Count != 1)
				{
					m_RecurringCharges.Clear();
					m_RecurringCharges.Add(new RecurringCharge(this, 0));
					changed = true;
				}
			}
			else
			{
				var endDate = DateTime.Now.AddDays(daysToForecast);

				var oldCharges = m_RecurringCharges.ToList();
				foreach (var charge in oldCharges)
					charge.Template = this;

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

				changed = !Enumerable.SequenceEqual(m_RecurringCharges, oldCharges);
			}

			if (changed)
				OnPropertyChanged(nameof(RecurringCharges));

			return m_RecurringCharges;
		}

		public RecurringChargeTemplate() { }
		public RecurringChargeTemplate(String name, DateTime date, double predefinedAmount)
		{
			Name = name;
			Date = date;
			PredefinedAmount = predefinedAmount;
		}
		public RecurringChargeTemplate(String name, DateTime date, double predefinedAmount, RecurrenceIntervals recurrenceInterval, UInt32 interval)
		{
			Name = name;
			Date = date;
			PredefinedAmount = predefinedAmount;
			RecurrenceInterval = recurrenceInterval;
			Interval = interval;
		}

		public void OnDeserialized()
		{
			uint i = 0;
			foreach (var item in m_RecurringCharges)
			{
				item.Template = this;
				item.Index = i++;
			}
		}

		String m_Name = "";
		DateTime m_Date = DateTime.Now;
		double m_PredefinedAmount = 0;
		RecurrenceIntervals m_RecurrenceInterval = RecurrenceIntervals.None;
		UInt32 m_Interval = 0;
		List<RecurringCharge> m_RecurringCharges = new();

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class RecurringCharge : BudgetEntry
	{
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
				else
					return m_DefinedAmount;
			}
			set
			{
				if (m_DefinedAmount == value)
					return;
				m_DefinedAmount = value == 0 ? null : value;
				OnPropertyChanged(nameof(Amount));
			}
		}

		public void ResetAmount()
		{
			m_DefinedAmount = Template.PredefinedAmount;
			OnPropertyChanged(nameof(Amount));
		}

		public RecurringCharge() { }
		public RecurringCharge(RecurringChargeTemplate template, UInt32 index)
		{
			Template = template;
			Index = index;
			ResetAmount();
		}

		internal UInt32 Index = 0;
		internal RecurringChargeTemplate Template = new();
		double? m_DefinedAmount = null;
	}
}
