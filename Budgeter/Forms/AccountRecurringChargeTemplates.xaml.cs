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

namespace Budgeter
{
    /// <summary>
    /// Interaction logic for AccountRecurringChargeTemplates.xaml
    /// </summary>
    public partial class AccountRecurringChargeTemplates : UserControl, INotifyPropertyChanged
    {
        BudgetView? m_BudgetView;
        public BudgetView? CurrentBudgetView
        {
            get { return m_BudgetView; }
            set
            {
                if (m_BudgetView != null)
                {
                    m_BudgetView.PropertyChanged -= OnBudgetViewPropertyChanged;
                }

                m_BudgetView = value;

                if (m_BudgetView != null)
                {
                    m_BudgetView.PropertyChanged += OnBudgetViewPropertyChanged;
                }

                NotifyPropertyChanged(nameof(CurrentBudgetView));
            }
        }

        Account? m_SelectedAccount;
        bool m_dataGrid_Templates_Updating = false;
        private void OnBudgetViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (m_BudgetView == null)
                return;

            if (e.PropertyName == nameof(BudgetView.SelectedAccount))
            {
                if (m_SelectedAccount != null)
                {
                    m_SelectedAccount.PropertyChanged -= OnSelectedAccountPropertyChanged;
                }

                m_SelectedAccount = m_BudgetView.SelectedAccount;
                NotifyPropertyChanged(nameof(CurrentBudgetView));
                NotifyPropertyChanged(nameof(BudgetView.SelectedAccount));

                if (m_SelectedAccount != null)
                {
                    NumericUpDown_DaysToForecast.Value = m_SelectedAccount.DaysToForecast;

                    m_SelectedAccount.PropertyChanged += OnSelectedAccountPropertyChanged;
                }
            }
            else if (e.PropertyName == nameof(BudgetView.SelectedRecurringChargeTemplates) && !m_dataGrid_Templates_Updating)
            {
                m_dataGrid_Templates_Updating = true;
                dataGrid_Templates.SelectedItems.Clear();
                var entries = m_BudgetView.SelectedRecurringChargeTemplates;
                if (entries != null)
                {
                    foreach (var entry in entries)
                        dataGrid_Templates.SelectedItems.Add(entry);
                }
                m_dataGrid_Templates_Updating = false;
            }
        }

        private void OnSelectedAccountPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (CurrentBudgetView == null)
                return;

        }



        private void DataGrid_Templates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_BudgetView == null || m_dataGrid_Templates_Updating)
                return;

            HashSet<RecurringChargeTemplate> entries = new();

            foreach (var item in dataGrid_Templates.SelectedItems)
            {
                if (item is RecurringChargeTemplate entry)
                    entries.Add(entry);
            }

            m_dataGrid_Templates_Updating = true;
            m_BudgetView.SelectedRecurringChargeTemplates = entries;
            m_dataGrid_Templates_Updating = false;
        }
        private void DataGrid_Templates_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

        }


        public AccountRecurringChargeTemplates()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var e = new PropertyChangedEventArgs(propertyName);
            PropertyChanged?.Invoke(this, e);
        }


        private void NumericUpDown_ValueChanged(object sender, NumericUpDownEventArgs e)
        {
            if (m_BudgetView != null && m_BudgetView.SelectedAccount != null)
                m_BudgetView.SelectedAccount.DaysToForecast = (int)e.NewValue;
        }

        private void OnMenuClick(object sender, RoutedEventArgs e)
		{
			MenuItem? menuItem = sender as MenuItem;

			if (menuItem == null || m_BudgetView == null || menuItem.Tag is not string menuItemTag)
				return;

			MenuClickHandlers.OnMenuClick(menuItemTag, m_BudgetView);
		}
    }
}
