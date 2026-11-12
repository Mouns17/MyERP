using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages
{
    public partial class SuppliesPage : Page
    {
        public SuppliesPage()
        {
            InitializeComponent();
            SuppliesFrame.Navigate(new Supplies.SuppliesTablePage());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(sender as Button);
            SuppliesFrame.Navigate(new Supplies.SuppliesTablePage());
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SetActiveTab(sender as Button);
            SuppliesFrame.Navigate(new Supplies.SuppliersPage());
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            SetActiveTab(sender as Button);
            SuppliesFrame.Navigate(new Supplies.OrderSupplierPage());
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            SetActiveTab(sender as Button);
            SuppliesFrame.Navigate(new Supplies.WarehousesPage());
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