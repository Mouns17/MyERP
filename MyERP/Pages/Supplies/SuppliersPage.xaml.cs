using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;


namespace MyERP.Pages.Supplies
{
    /// <summary>
    /// Логика взаимодействия для SuppliersPage.xaml
    /// </summary>
    public partial class SuppliersPage : Page
    {
        private DataTable suppliersTable;
        public SuppliersPage()
        {
            InitializeComponent();
            LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
                SELECT
                    pos.id_поставщика AS Id,
                    pos.Наименование AS Наименование,
                    pos.ИНН AS ИНН,
                    pos.Телефон AS Телефон,
                    pos.Электронная_почта AS ЭлПочта,
                    pos.Адрес AS Адрес
                FROM поставщики AS pos";
                    using (MySqlCommand command =
                           new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter =
                           new MySqlDataAdapter(command))
                    {
                        suppliersTable = new DataTable();
                        adapter.Fill(suppliersTable);

                        SuppliersDataGrid.ItemsSource =
                            suppliersTable.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить поставщиков:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
