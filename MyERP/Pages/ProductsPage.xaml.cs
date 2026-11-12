using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages
{
    public partial class ProductsPage : Page
    {
        public ProductsPage()
        {
            InitializeComponent();
            ProductsFrame.Navigate(new Products.ProductsTablePage());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(sender as Button);
            ProductsFrame.Navigate(new Products.ProductsTablePage());
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SetActiveTab(sender as Button);
            ProductsFrame.Navigate(new Products.CategoriesPage());
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