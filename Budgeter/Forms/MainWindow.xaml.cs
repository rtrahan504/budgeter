using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Design;


namespace Budgeter
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, IBudgetCommands, INotifyPropertyChanged, INotifyPropertyChanging
	{
		public MainWindow()
		{
			InitializeComponent();

			BudgetView = m_BudgetView;

			PropertyChanging += OnPropertyChanging;
			PropertyChanged += OnPropertyChanged;
		}


		readonly String RegistryPath_CurrentFile = @"SOFTWARE\Budgeter";
		readonly System.Uri DarkModeSource = new("pack://application:,,,/Selen.Wpf.SystemStyles;component/Styles.xaml");

		BudgetView m_BudgetView = new();

		public BudgetView BudgetView
		{
			get { return m_BudgetView; }
			set
			{
				NotifyPropertyChanging();

				m_AccountList.BudgetView = null;
				m_AccountBalanceSheet.BudgetView = null;
				m_AccountBalanceChart.BudgetView = null;
				m_AccountRecurringChargeTemplates.BudgetView = null;

				m_BudgetView.PropertyChanged -= OnBudgetViewPropertyChanged;

				m_BudgetView = value;

				m_BudgetView.PropertyChanged += OnBudgetViewPropertyChanged;
				m_AccountList.BudgetView = m_BudgetView;
				m_AccountBalanceSheet.BudgetView = m_BudgetView;
				m_AccountBalanceChart.BudgetView = m_BudgetView;
				m_AccountRecurringChargeTemplates.BudgetView = m_BudgetView;


				NotifyPropertyChanged();
			}
		}

		public String DynamicTitle
		{
			get
			{
				if (BudgetView.CurrentFile == "")
					return "Budgeter - {untitled}";
				else
					return "Budgeter - " + System.IO.Path.GetFileName(BudgetView.CurrentFile) + (BudgetView.IsBudgetModified ? "*" : "");
			}
		}


		public event PropertyChangingEventHandler? PropertyChanging;
		public event PropertyChangedEventHandler? PropertyChanged;

		private void NotifyPropertyChanging([CallerMemberName] String propertyName = "")
		{
			PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
		}
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		void OnPropertyChanging(object? sender, PropertyChangingEventArgs e)
		{
			if (e.PropertyName == nameof(BudgetView))
			{
				if (!BudgetView.IsBudgetModified)
					return;

				var res = MessageBox.Show("Save the changes to the current budget?", "Budget Modified", MessageBoxButton.YesNoCancel);

				if (res == MessageBoxResult.Cancel)
					throw new OperationCanceledException();
				else if (res == MessageBoxResult.Yes)
					SaveBudget(false);
			}
		}
		void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BudgetView))
			{
				OnBudgetViewPropertyChanged(sender, new PropertyChangedEventArgs(nameof(BudgetView.CurrentFile)));
			}
		}



		private void OnBudgetViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BudgetView.IsBudgetModified))
			{
				NotifyPropertyChanged(nameof(DynamicTitle));
			}
			else if (e.PropertyName == nameof(BudgetView.CurrentFile))
			{
				var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegistryPath_CurrentFile);
				key.SetValue("CurrentFile", BudgetView == null || BudgetView.CurrentFile == "" ? "" : BudgetView.CurrentFile);
				key.Close();

				NotifyPropertyChanged(nameof(BudgetView.CurrentFile));
				NotifyPropertyChanged(nameof(DynamicTitle));
			}
		}



		private void Window_Closing(object sender, CancelEventArgs e)
		{
			try
			{
				PropertyChanged -= OnPropertyChanged; // Disconnect so the registry doesn't store new object as the last active file
				BudgetView = new(); // Set to new to check if user wants to save changes
			}
			catch (OperationCanceledException)
			{
				PropertyChanged += OnPropertyChanged; // Back to normal since canceled
				e.Cancel = true;
				return;
			}

			Microsoft.Win32.RegistryKey? key = null;
			try
			{
				key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegistryPath_CurrentFile);
				if (key == null)
					return;
				key.SetValue("WindowWidth", Width);
				key.SetValue("WindowHeight", Height);
				key.SetValue("WindowState", WindowState.ToString());
				key.SetValue("WindowX", Left);
				key.SetValue("WindowY", Top);
				key.SetValue("AccountSplitter", Grid_BalanceSheet.ColumnDefinitions[0].Width.Value.ToString());
				key.SetValue("BalanceSheetSplitter", (Grid_BalanceSheet.ColumnDefinitions[2].Width.Value / Grid_BalanceSheet.ColumnDefinitions[4].Width.Value).ToString());
				key.SetValue("ColorMode", toggleButton_DarkMode.IsChecked == true ? "Light" : "Dark");
			}
			catch
			{ }
			finally
			{
				key?.Close();
			}
		}

		void SaveBudget(bool forceSaveAs)
		{
			if (!forceSaveAs && BudgetView.CurrentFile != "")
			{
				BudgetView.Save(BudgetView.CurrentFile);
			}
			else
			{
				Microsoft.Win32.SaveFileDialog dialog = new()
				{
					FileName = "Budget", // Default file name
					DefaultExt = ".bgt", // Default file extension
					Filter = "Budget File (.bgt)|*.bgt" // Filter files by extension
				};

				if (dialog.ShowDialog() == true)
				{
					BudgetView.Save(dialog.FileName);
				}
			}
		}

		private void Window_SourceInitialized(object sender, EventArgs e)
		{
			Microsoft.Win32.RegistryKey? key = null;
			try
			{
				key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryPath_CurrentFile);
				if (key == null)
					return;

				var file = key.GetValue("CurrentFile") as string ?? "";

				double val = Width;
				if (Double.TryParse(key.GetValue("WindowWidth")?.ToString(), out val))
					Width = val;
				val = Height;
				if (Double.TryParse(key.GetValue("WindowHeight")?.ToString(), out val))
					Height = val;
				val = Left;
				if (Double.TryParse(key.GetValue("WindowX")?.ToString(), out val))
					Left = val;
				val = Top;
				if (Double.TryParse(key.GetValue("WindowY")?.ToString(), out val))
					Top = val;

				object? windowState = WindowState;
				if (WindowState.TryParse(typeof(WindowState), key.GetValue("WindowState")?.ToString(), true, out windowState))
					WindowState = windowState is WindowState state ? state : WindowState;

				if (Double.TryParse(key.GetValue("AccountSplitter") as string, out val))
					Grid_BalanceSheet.ColumnDefinitions[0].Width = new GridLength(val, GridUnitType.Pixel);
				if (Double.TryParse(key.GetValue("BalanceSheetSplitter") as string, out val))
					Grid_BalanceSheet.ColumnDefinitions[2].Width = new GridLength(val, GridUnitType.Star);

				toggleButton_DarkMode.IsChecked = key.GetValue("ColorMode")?.ToString() == "Light";

				BudgetView = BudgetView.Load(file);
			}
			catch
			{
			}
			finally
			{
				key?.Close();
			}
		}

		private void ToggleButton_Checked(object sender, RoutedEventArgs e)
		{
			foreach (var control in new Control[] { this, this.m_AccountList, this.m_AccountBalanceSheet, this.m_AccountRecurringChargeTemplates, this.m_AccountBalanceChart })
			{
				bool found = false;
				do
				{
					found = false;
					foreach (var dict in control.Resources.MergedDictionaries)
					{
						if (dict != null && dict.Source.OriginalString == DarkModeSource.OriginalString)
						{
							control.Resources.MergedDictionaries.Remove(dict);
							found = true;
							break;
						}
					}
				} while (found);
			}

			UpdateLayout();

			m_AccountBalanceChart.UpdateStyling();
		}

		private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
		{
			var rd = new System.Windows.ResourceDictionary()
			{
				Source = DarkModeSource
			};
			Resources.MergedDictionaries.Add(rd);

			UpdateLayout();

			m_AccountBalanceChart.UpdateStyling();
		}

		private void Label_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			m_Slider_Name.Value = 1;
		}


		void OnCommand_New(object sender, ExecutedRoutedEventArgs args)
		{
			try
			{
				if (System.Windows.MessageBox.Show("Are you sure you want to start a new budget?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
					BudgetView = new BudgetView();
			}
			catch (OperationCanceledException)
			{
				return;
			}
		}
		void OnCommand_Open(object sender, ExecutedRoutedEventArgs args)
		{
			try
			{
				Microsoft.Win32.OpenFileDialog dialog = new()
				{
					FileName = "Budget", // Default file name
					DefaultExt = ".bgt", // Default file extension
					Filter = "Budget File (.bgt)|*.bgt" // Filter files by extension
				};

				if (dialog.ShowDialog() == true)
					BudgetView = BudgetView.Load(dialog.FileName);
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (Exception ex)
			{
				MessageBox.Show("An error occurred.\n" + ex.Message, "Error", MessageBoxButton.OK);
			}
		}
		void OnCommand_Save(object sender, ExecutedRoutedEventArgs args)
		{
			SaveBudget(false);
		}
		void OnCommand_SaveAs(object sender, ExecutedRoutedEventArgs args)
		{
			SaveBudget(true);
		}
		void OnCommand_Close(object sender, ExecutedRoutedEventArgs args)
		{
			Close();
		}

		public static RoutedCommand Command_About = new RoutedCommand();
		void OnCommand_About(object sender, ExecutedRoutedEventArgs e)
		{
			About newwindow = new()
			{
				Owner = this
			};

			if (toggleButton_DarkMode.IsChecked == false)
			{
				var rd = new System.Windows.ResourceDictionary
				{
					Source = DarkModeSource
				};
				newwindow.Resources.MergedDictionaries.Add(rd);
			}

			newwindow.ShowDialog();
		}


		public void OnCommand_Account_NewAccount(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).Account_NewAccount(sender, e);
		public void OnCommand_Account_DeleteAccount(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).Account_DeleteAccount(sender, e);
		public void OnCommand_BalanceSheet_Refresh(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_Refresh(sender, e);
		public void OnCommand_BalanceSheet_NewCharge(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_NewCharge(sender, e);
		public void OnCommand_BalanceSheet_NewBalanceOverride(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_NewBalanceOverride(sender, e);
		public void OnCommand_BalanceSheet_Activate(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_Activate(sender, e);
		public void OnCommand_BalanceSheet_Deactivate(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_Deactivate(sender, e);
		public void OnCommand_BalanceSheet_Delete(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_Delete(sender, e);
		public void OnCommand_BalanceSheet_ResetAmount(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).BalanceSheet_ResetAmount(sender, e);
		public void OnCommand_RecurringTransactions_New(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).RecurringTransactions_New(sender, e);
		public void OnCommand_RecurringTransactions_Delete(object sender, ExecutedRoutedEventArgs e) => ((IBudgetCommands)this).RecurringTransactions_Delete(sender, e);
	}
}