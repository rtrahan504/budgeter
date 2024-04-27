using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Budgeter
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged, INotifyPropertyChanging
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

				m_AccountList.CurrentBudgetView = null;
				m_AccountBalanceSheet.CurrentBudgetView = null;
				m_AccountBalanceChart.CurrentBudgetView = null;
				m_AccountRecurringChargeTemplates.CurrentBudgetView = null;
				m_BudgetView.PropertyChanged -= OnBudgetViewPropertyChanged;

				m_BudgetView = value;

				m_BudgetView.PropertyChanged += OnBudgetViewPropertyChanged;
				m_AccountList.CurrentBudgetView = m_BudgetView;
				m_AccountBalanceSheet.CurrentBudgetView = m_BudgetView;
				m_AccountBalanceChart.CurrentBudgetView = m_BudgetView;
				m_AccountRecurringChargeTemplates.CurrentBudgetView = m_BudgetView;

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


		private void OnMenuClick(object sender, RoutedEventArgs e)
		{
			MenuItem? menuItem = sender as MenuItem;

			if (menuItem == null || menuItem.Tag is not string menuItemTag)
				return;

			MenuClickHandlers.OnMenuClick(menuItemTag, BudgetView);

			if (menuItemTag == "File_New")
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
			else if (menuItemTag == "File_Open")
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
			else if (menuItemTag == "File_Save" || menuItemTag == "File_SaveAs")
			{
				SaveBudget(menuItemTag == "File_SaveAs");
			}
			else if (menuItemTag == "Program_Exit")
			{
				Close();
			}
			else if (menuItemTag == "About")
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
			foreach( var control in new Control[] { this, this.m_AccountList, this.m_AccountBalanceSheet, this.m_AccountRecurringChargeTemplates, this.m_AccountBalanceChart })
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
	}
}
