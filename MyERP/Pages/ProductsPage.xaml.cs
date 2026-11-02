using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;


namespace MyERP.Pages
{
    /// <summary>
    /// Логика взаимодействия для ProductsPage.xaml
    /// </summary>
    public partial class ProductsPage : Page
    {
        public ProductsPage()
        {
            InitializeComponent();
            ProductsFrame.Navigate(new Products.ProductsTablePage());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ProductsFrame.Navigate(new Products.ProductsTablePage());
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ProductsFrame.Navigate(new Products.CategoriesPage());
        }
    }
}
