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
        private DataTable productsTable;
        public ProductsPage()
        {
            InitializeComponent();
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            t.id_товара AS Id,
                            t.Артикул AS Артикул,
                            t.Наименование AS Наименование,
                            t.Цена AS Цена
                        FROM ТОВАР AS t
                        ORDER BY t.Наименование";

                    using (MySqlCommand command =
                           new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter =
                           new MySqlDataAdapter(command))
                    {
                        productsTable = new DataTable();
                        adapter.Fill(productsTable);

                        ProductsDataGrid.ItemsSource =
                            productsTable.DefaultView;
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить товары:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
