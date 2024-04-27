using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;

namespace Budgeter
{
	/// <summary>
	/// Interaction logic for AccountBalanceChart.xaml
	/// </summary>
	public partial class AccountBalanceChart : UserControl, INotifyPropertyChanged
	{
		BudgetView? m_BudgetView;
		Account? m_SelectedAccount;
		bool m_SelectedAccountEntriesUpdating = false;

		DateTime m_StartDate = DateTime.Now.AddMonths(-6);
		DateTime m_EndDate = DateTime.Now.AddMonths(1);
		public OxyPlot.PlotModel PlotModel { get; private set; }
		DateTimeAxis m_XAxis;
		LinearAxis m_YAxis;
		LineAnnotation m_TodayAnnotation = new() { Type = LineAnnotationType.Vertical, LineStyle = OxyPlot.LineStyle.Solid, StrokeThickness = 3 };
		bool m_TodaySelected = false;

		ScatterSeries? m_SelectedSeries;

		public DateTime StartDate
		{
			get { return m_StartDate; }
			set { m_StartDate = value; updatePlot(); }
		}
		public DateTime EndDate
		{
			get { return m_EndDate; }
			set { m_EndDate = value; updatePlot(); }
		}

		public BudgetView? BudgetView
		{
			get { return m_BudgetView; }
			set
			{
				if (m_BudgetView != null)
				{
					m_BudgetView.PropertyChanged -= OnBudgetViewPropertyChanged;
				}

				SelectedAccount = null;
				m_BudgetView = value;

				NotifyPropertyChanged(nameof(BudgetView));

				if (m_BudgetView != null)
				{
					m_BudgetView.PropertyChanged += OnBudgetViewPropertyChanged;
					SelectedAccount = m_BudgetView.SelectedAccount;
				}
			}
		}

		public Account? SelectedAccount
		{
			get { return m_SelectedAccount; }
			set
			{
				if (m_SelectedAccount != null)
				{
					m_SelectedAccount.PropertyChanged -= OnSelectedAccountPropertyChanged;
				}

				m_SelectedAccount = m_BudgetView?.SelectedAccount;
				updatePlot();

				if (m_SelectedAccount != null)
				{
					m_SelectedAccount.PropertyChanged += OnSelectedAccountPropertyChanged;
				}

				NotifyPropertyChanged(nameof(SelectedAccount));
			}
		}

		private void OnBudgetViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (m_BudgetView == null)
				return;

			if (e.PropertyName == nameof(BudgetView.SelectedAccount))
			{
				SelectedAccount = m_BudgetView?.SelectedAccount;

				updatePlot();
			}
			else if (e.PropertyName == nameof(BudgetView.SelectedAccountEntries) && !m_SelectedAccountEntriesUpdating)
			{
				updateSelected();
				PlotModel.InvalidatePlot(true);
			}
		}

		private void OnSelectedAccountPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			Application.Current.Dispatcher.BeginInvoke(
			  DispatcherPriority.Background,
			  () =>
			  {
				  try
				  {
					  if (e.PropertyName == "Balance" || e.PropertyName == "Entries")
					  {
						  updatePlot();
					  }
				  }
				  catch
				  { }
			  });
		}

		System.Windows.Threading.DispatcherTimer m_DispatcherTimer = new System.Windows.Threading.DispatcherTimer();


		public AccountBalanceChart()
		{
			m_XAxis = new DateTimeAxis { Position = AxisPosition.Bottom, Minimum = DateTimeAxis.ToDouble(m_StartDate), Maximum = DateTimeAxis.ToDouble(m_EndDate) };
			m_YAxis = new LinearAxis { Position = AxisPosition.Left };
			PlotModel = new OxyPlot.PlotModel { 
				Annotations = { m_TodayAnnotation }, 
				Axes = { m_XAxis, m_YAxis } };

#pragma warning disable CS0618
			m_XAxis.AxisChanged += XAxis_AxisChanged;
#pragma warning restore CS0618

			InitializeComponent();

			plotView_Balance.HideTracker();

			UpdateStyling();

			m_DispatcherTimer.Tick += updateToday;
			m_DispatcherTimer.Interval = new TimeSpan(0, 1, 0);
			m_DispatcherTimer.Start();
		}
		public void UpdateStyling()
		{
			var foreColor = this.Foreground.ToOxyColor();

			foreach (var axis in PlotModel.Axes)
			{
				axis.AxislineColor = foreColor;
				axis.ExtraGridlineColor = foreColor;
				axis.MajorGridlineColor = foreColor;
				axis.MinorGridlineColor = foreColor;
				axis.MinorTicklineColor = foreColor;
				axis.TextColor = foreColor;
				axis.TicklineColor = foreColor;
				axis.TitleColor = foreColor;

				axis.MajorGridlineStyle = OxyPlot.LineStyle.Dot;
				axis.MajorGridlineThickness = 0.5;
			}

			PlotModel.TextColor = foreColor;
			PlotModel.SubtitleColor = foreColor;
			PlotModel.PlotAreaBorderColor = foreColor;

			PlotModel.InvalidatePlot(false);
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			var e = new PropertyChangedEventArgs(propertyName);
			PropertyChanged?.Invoke(this, e);
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (comboBox_ChartInterval != null && label_ChartInterval != null && comboBox_ChartType != null)
			{
				comboBox_ChartInterval.Visibility = comboBox_ChartType.SelectedItem == comboBox_ChartType_Balance ? Visibility.Collapsed : Visibility.Visible;
				label_ChartInterval.Visibility = comboBox_ChartInterval.Visibility;
			}

			updatePlot();
		}

		private DateTime GetNextTime(DateTime currentTime)
		{
			if (comboBox_ChartInterval.SelectedItem == comboBox_ChartInterval_Monthly)
			{
				currentTime = currentTime.AddDays(-currentTime.Day + m_StartDate.Day);
				return currentTime.AddMonths(1);
			}
			else if (comboBox_ChartInterval.SelectedItem == comboBox_ChartInterval_Daily)
			{
				return currentTime.AddDays(1);
			}

			return DateTime.MaxValue;
		}

		private void updatePlot()
		{
			PlotModel.Series.Clear();

			if (m_SelectedAccount == null)
			{
				PlotModel.InvalidatePlot(true);
				return;
			}

			if (comboBox_ChartType.SelectedItem == comboBox_ChartType_Balance)
			{
				var lineSeries = new StairStepSeries { MarkerType = OxyPlot.MarkerType.None, Color = OxyColor.FromRgb(255, 165, 0) };
				var positiveSeries = new ScatterSeries { MarkerType = OxyPlot.MarkerType.Circle, MarkerFill = OxyColor.FromRgb(0, 120, 0) };
				var negativeSeries = new ScatterSeries { MarkerType = OxyPlot.MarkerType.Circle, MarkerFill = OxyColor.FromRgb(120, 0, 0) };

				foreach (var item in m_SelectedAccount.Entries.Reverse())
				{
					if (item is Today)
						continue;

					lineSeries.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(item.Date), item.Balance));

					if (item.Balance > 0)
						positiveSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(item.Date), item.Balance, 5.0, 0) { Tag = item });
					else if (item.Balance < 0)
						negativeSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(item.Date), item.Balance, 5.0, 0) { Tag = item });
				}

				PlotModel.Series.Add(lineSeries);
				PlotModel.Series.Add(positiveSeries);
				PlotModel.Series.Add(negativeSeries);
			}
			else if (
				(
				comboBox_ChartType.SelectedItem == comboBox_ChartType_CashFlow || 
				comboBox_ChartType.SelectedItem == comboBox_ChartType_Income || 
				comboBox_ChartType.SelectedItem == comboBox_ChartType_Expense
				) && comboBox_ChartInterval.SelectedItem != comboBox_ChartInterval_Transaction)
			{
				DateTime currentTime = m_StartDate;
				DateTime nextTime = GetNextTime(currentTime);
				double currentValue = 0;

				var barSeries = new RectangleBarSeries { };
				foreach (var item in m_SelectedAccount.Entries.Reverse())
				{
					if (item is Today || item.Amount == null || item.Date < currentTime)
						continue;
					
					while (item.Date >= nextTime)
					{
						if (!double.IsNaN(currentValue))
                            barSeries.Items.Add(new RectangleBarItem(DateTimeAxis.ToDouble(currentTime), 0, DateTimeAxis.ToDouble(nextTime), currentValue) { Color = currentValue > 0 ? OxyColor.FromRgb(0, 120, 0) : OxyColor.FromRgb(120, 0, 0) });

						currentTime = nextTime;
						nextTime = GetNextTime(currentTime);
						currentValue = double.NaN;
					}
					
					if (
						(comboBox_ChartType.SelectedItem == comboBox_ChartType_CashFlow) ||
						(comboBox_ChartType.SelectedItem == comboBox_ChartType_Income && item.Amount.Value > 0) ||
						(comboBox_ChartType.SelectedItem == comboBox_ChartType_Expense && item.Amount.Value < 0)
						)
					{
						if (double.IsNaN(currentValue))
							currentValue = item.Amount.Value;
						else
							currentValue += item.Amount.Value;
					}
				}

				if (!double.IsNaN(currentValue))
                    barSeries.Items.Add(new RectangleBarItem(DateTimeAxis.ToDouble(currentTime), 0, DateTimeAxis.ToDouble(nextTime), currentValue) { Color = currentValue > 0 ? OxyColor.FromRgb(0, 120, 0) : OxyColor.FromRgb(120, 0, 0) });

				PlotModel.Series.Add(barSeries);
			}
			else if (
				(
				comboBox_ChartType.SelectedItem == comboBox_ChartType_CashFlow || 
				comboBox_ChartType.SelectedItem == comboBox_ChartType_Income || 
				comboBox_ChartType.SelectedItem == comboBox_ChartType_Expense
				) && comboBox_ChartInterval.SelectedItem == comboBox_ChartInterval_Transaction)
			{
				var positiveSeries = new ScatterSeries { MarkerType = OxyPlot.MarkerType.Circle, MarkerFill = OxyColor.FromRgb(0, 120, 0) };
				var negativeSeries = new ScatterSeries { MarkerType = OxyPlot.MarkerType.Circle, MarkerFill = OxyColor.FromRgb(120, 0, 0) };

				foreach (var item in m_SelectedAccount.Entries.Reverse())
				{
					if (item is Today || item.Amount == null)
						continue;

					if (
						(comboBox_ChartType.SelectedItem == comboBox_ChartType_CashFlow) ||
						(comboBox_ChartType.SelectedItem == comboBox_ChartType_Income && item.Amount.Value > 0) ||
						(comboBox_ChartType.SelectedItem == comboBox_ChartType_Expense && item.Amount.Value < 0)
						)
					{
						if (item.Amount.Value > 0)
							positiveSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(item.Date), item.Amount.Value, 5.0, 0) { Tag = item });
						else if (item.Amount.Value < 0)
							negativeSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(item.Date), item.Amount.Value, 5.0, 0) { Tag = item });
					}
				}

				PlotModel.Series.Add(positiveSeries);
				PlotModel.Series.Add(negativeSeries);
			}

			m_XAxis.Minimum = DateTimeAxis.ToDouble(m_StartDate);
			m_XAxis.Maximum = DateTimeAxis.ToDouble(m_EndDate);

			updateSelected();
			PlotModel.ResetAllAxes();
			PlotModel.InvalidatePlot(true);
		}

		private void updateSelected()
		{
			if (m_SelectedSeries != null)
				PlotModel.Series.Remove(m_SelectedSeries);

			m_TodaySelected = false;

			var selectedSeries = new ScatterSeries { 
				MarkerType = OxyPlot.MarkerType.Circle, 
				MarkerFill = OxyPlot.OxyColor.FromRgb(SystemColors.HighlightColor.R, SystemColors.HighlightColor.G, SystemColors.HighlightColor.B),
				MarkerStroke = OxyPlot.OxyColor.FromRgb(SystemColors.HighlightTextColor.R, SystemColors.HighlightTextColor.G, SystemColors.HighlightTextColor.B)
			};
			if (m_BudgetView != null)
			{
				foreach (var item in m_BudgetView.SelectedAccountEntries)
				{
					if (item is Today)
					{
						m_TodaySelected = true;
						continue;
					}

					if (comboBox_ChartType.SelectedItem == comboBox_ChartType_Balance)
					{
						selectedSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(item.Date), item.Balance, 7.0, 10));
					}
					else
					{
						if (item.Amount != null)
							selectedSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(item.Date), item.Amount.Value, 7.0, 10));
					}
				}
			}

			updateToday(this, new EventArgs());
			PlotModel.Series.Add(selectedSeries);
			m_SelectedSeries = selectedSeries;
		}

		private void updateToday(object? sender, EventArgs e)
		{
			m_TodayAnnotation.X = DateTimeAxis.ToDouble(DateTime.Now);
			m_TodayAnnotation.Color = m_TodaySelected ? OxyPlot.OxyColor.FromRgb(SystemColors.HighlightColor.R, SystemColors.HighlightColor.G, SystemColors.HighlightColor.B) : OxyPlot.OxyColor.FromRgb(0, 0, 255);

			PlotModel.InvalidatePlot(false);
		}

		private void XAxis_AxisChanged(object? sender, AxisChangedEventArgs e)
		{
			updateToday(this, new EventArgs());

			var t1 = DateTimeAxis.ToDateTime(m_XAxis.ActualMinimum);
			var t2 = DateTimeAxis.ToDateTime(m_XAxis.ActualMaximum);
			var seconds = Math.Max(1, (int)Math.Ceiling((t2 - t1).TotalSeconds / this.ActualWidth));

			m_DispatcherTimer.Interval = new TimeSpan(0, 0, seconds);
		}

		private void plotView_Balance_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (m_BudgetView == null)
				return;

			HashSet<AccountEntry> entries = new();

			var position = e.GetPosition(plotView_Balance).ToScreenPoint();
			var result = PlotModel.HitTest(new HitTestArguments(position, 5.0));
			foreach (var item in result)
			{
				if (item.Element is ScatterSeries scatterSeries)
				{
					var point = scatterSeries.Points[(int)item.Index];
					if (point?.Tag is AccountEntry entry)
					{
						entries.Add(entry);
					}
				}
				else if (item.Element == m_TodayAnnotation && m_BudgetView.SelectedAccount != null)
				{
					entries.Add(m_BudgetView.SelectedAccount.Today);
				}
			}

            m_BudgetView.SelectedAccountEntries = entries;
		}
	}
}
