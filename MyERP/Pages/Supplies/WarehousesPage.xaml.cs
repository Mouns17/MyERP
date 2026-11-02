using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages.Supplies
{
    /// <summary>
    /// Логика взаимодействия для WarehousesPage.xaml
    /// </summary>
    public partial class WarehousesPage : Page
    {
        private DataTable warehousesTable;
        private DataRow newWarehouseRow;
        public WarehousesPage()
        {
            InitializeComponent();
            LoadWarehouses();
        }

        private void LoadWarehouses()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
                    SELECT
                        w.id_склада AS Id,
                        w.Наименование AS Наименование,
                        w.адрес AS Адрес
                    FROM СКЛАД AS w
                    ORDER BY w.Наименование";

                    using (MySqlCommand command =
                           new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter =
                           new MySqlDataAdapter(command))
                    {
                        warehousesTable = new DataTable();
                        adapter.Fill(warehousesTable);

                        WarehousesDataGrid.ItemsSource =
                            warehousesTable.DefaultView;
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить склады:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveWarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            if (newWarehouseRow == null)
                return;

            // Завершаем редактирование DataGrid
            WarehousesDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            WarehousesDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            string name = newWarehouseRow["Наименование"].ToString().Trim();
            string address = newWarehouseRow["Адрес"].ToString().Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите наименование склада.");
                return;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("Введите адрес склада.");
                return;
            }

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
INSERT INTO СКЛАД
(Наименование, Адрес)
VALUES
(@name, @address)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@address", address);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    "Склад успешно добавлен!",
                    "Сохранение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                newWarehouseRow = null;

                SaveCategoryButton.Visibility = Visibility.Collapsed;

                LoadWarehouses();

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить склады:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddWarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            if (newWarehouseRow != null)
            {
                MessageBox.Show("Сначала сохраните текущую категорию .");
                return;
            }

            newWarehouseRow = warehousesTable.NewRow();

            newWarehouseRow["Id"] = DBNull.Value;
            newWarehouseRow["Наименование"] = "";
            newWarehouseRow["Адрес"] = "";

            warehousesTable.Columns["Id"].AllowDBNull = true;

            warehousesTable.Rows.Add(newWarehouseRow);

            WarehousesDataGrid.SelectedItem = newWarehouseRow;
            WarehousesDataGrid.ScrollIntoView(newWarehouseRow);

            SaveCategoryButton.Visibility = Visibility.Visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
