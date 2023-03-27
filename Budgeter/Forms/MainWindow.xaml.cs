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
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		readonly String RegistryPath_CurrentFile = @"SOFTWARE\Budgeter";

		Budget? m_Budget;
		BudgetView m_BudgetView = new();

		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		String m_CurrentFile = "";
		public String CurrentFile
		{
			get { return m_CurrentFile; }
			set
			{
				var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegistryPath_CurrentFile);
				key.SetValue("CurrentFile", value == "{untitled}" ? "" : value);
				key.Close();

				m_CurrentFile = value;
				NotifyPropertyChanged(nameof(DynamicTitle));
			}
		}
		public String DynamicTitle
		{
			get
			{
				if (CurrentFile == null || CurrentFile == "" || m_BudgetView == null)
					return "Budgeter";
				else
					return "Budgeter - " + System.IO.Path.GetFileName(CurrentFile) + (m_BudgetView.Budget.IsBudgetModified ? "*" : "");
			}
		}


		public MainWindow()
		{
			m_CurrentFile = "{untitled}";
			NotifyPropertyChanged(nameof(DynamicTitle));

			m_BudgetView.PropertyChanged += OnBudgetViewPropertyChanged;

			InitializeComponent();

			m_AccountList.CurrentBudgetView = m_BudgetView;
			m_AccountBalanceSheet.CurrentBudgetView = m_BudgetView;
			m_AccountRecurringChargeTemplates.CurrentBudgetView = m_BudgetView;

			m_BudgetView.Budget = new();
		}

		void OnBudgetPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Budget.IsBudgetModified))
			{
				NotifyPropertyChanged(nameof(DynamicTitle));
			}
		}

		void OnBudgetViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BudgetView.Budget))
			{
				if (m_Budget != null)
					m_Budget.PropertyChanged -= OnBudgetPropertyChanged;

				m_Budget = m_BudgetView.Budget;

				if (m_Budget != null)
					m_Budget.PropertyChanged += OnBudgetPropertyChanged;
			}
		}


		private void OnMenuClick(object sender, RoutedEventArgs e)
		{
			MenuItem? menuItem = sender as MenuItem;

			if (menuItem == null || menuItem.Tag is not string menuItemTag)
				return;

			MenuClickHandlers.OnMenuClick(menuItemTag, m_BudgetView);

			if (menuItemTag == "File_New")
			{
				if (System.Windows.MessageBox.Show("Are you sure you want to start a new budget?", "Confirm", System.Windows.MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					m_BudgetView.Budget = new Budget();
					CurrentFile = "{untitled}";
				}
			}
			else if (menuItemTag == "File_Open")
			{
				Microsoft.Win32.OpenFileDialog dialog = new()
				{
					FileName = "Budget", // Default file name
					DefaultExt = ".bgt", // Default file extension
					Filter = "Budget File (.bgt)|*.bgt" // Filter files by extension
				};

				if (dialog.ShowDialog() == true)
				{
					try
					{
						var newBudget = Budget.Load(dialog.FileName);
						if (newBudget != null)
						{
							m_BudgetView.Budget = newBudget;
							CurrentFile = dialog.FileName;
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show("An error occurred.\n" + ex.Message, "Error", MessageBoxButton.OK);
					}
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
						Source = new System.Uri("pack://application:,,,/Selen.Wpf.SystemStyles;component/Styles.xaml")
					};
					newwindow.Resources.MergedDictionaries.Add(rd);
				}

				newwindow.ShowDialog();
			}
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (m_BudgetView == null)
				return;

			if (m_BudgetView.Budget.IsBudgetModified)
			{
				var res = MessageBox.Show("Save the changes to the current budget?", "Budget Modified", MessageBoxButton.YesNoCancel);
				if (res == MessageBoxResult.Cancel)
				{
					e.Cancel = true;
					return;
				}
				else if (res == MessageBoxResult.Yes)
				{
					SaveBudget(false);
				}
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
			if (m_BudgetView == null)
				return;

			if (!forceSaveAs && m_CurrentFile != "")
			{
				m_BudgetView.Budget.Save(m_CurrentFile);

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
					m_BudgetView.Budget.Save(dialog.FileName);

					CurrentFile = dialog.FileName;
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

				var newBudget = Budget.Load(file);
				if (newBudget != null)
				{
					m_CurrentFile = file;
					NotifyPropertyChanged(nameof(DynamicTitle));
					m_BudgetView.Budget = newBudget;
				}
			}
			catch
			{ }
			finally
			{
				key?.Close();
			}
		}

		private void ToggleButton_Checked(object sender, RoutedEventArgs e)
		{
			foreach (var dict in Resources.MergedDictionaries)
			{
				if (dict != null && dict.Source.OriginalString == "pack://application:,,,/Selen.Wpf.SystemStyles;component/Styles.xaml")
				{
					Resources.MergedDictionaries.Remove(dict);

					UpdateLayout();

					break;
				}
			}
		}

		private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
		{
			var rd = new System.Windows.ResourceDictionary()
			{
				Source = new System.Uri("pack://application:,,,/Selen.Wpf.SystemStyles;component/Styles.xaml")
			};
			Resources.MergedDictionaries.Add(rd);
		}

		private void Label_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			m_Slider_Name.Value = 1;
		}
	}
}
