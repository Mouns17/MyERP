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
            SetActiveTab(sender as Button);
            SalesFrame.Navigate(new Sales.SalesListPage());
        }

        private void CustomersButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(sender as Button);
            SalesFrame.Navigate(new Sales.CustomersPage());
        }

        private void SetActiveTab(Button clickedButton)
        {
            if (clickedButton == null) return;

            if (clickedButton.Parent is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Button btn)
                        btn.Tag = null;
                }
            }

            clickedButton.Tag = "Active";
        }
    }
}