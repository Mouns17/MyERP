using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MyERP.Pages
{
    public partial class SalesPage : Page
    {
        public SalesPage()
        {
            InitializeComponent();
            SalesFrame.Navigate(new Sales.SalesListPage());
        }

        private void SalesListButton_Click(object sender, RoutedEventArgs e)
        {
            SalesFrame.Navigate(new Sales.SalesListPage());
        }

        private void CustomersButton_Click(object sender, RoutedEventArgs e)
        {
            SalesFrame.Navigate(new Sales.CustomersPage());
        }
    }
}