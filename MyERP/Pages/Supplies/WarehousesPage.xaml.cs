using MyERP.Classes;
using MySqlConnector;
using System;
using MyERP.Pages;
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

                    string query = @"SELECT w.id_склада AS Id, w.Наименование AS Наименование, w.адрес AS Адрес FROM СКЛАД AS w ORDER BY w.Наименование";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        warehousesTable = new DataTable();
                        adapter.Fill(warehousesTable);

                        WarehousesDataGrid.ItemsSource = warehousesTable.DefaultView;
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить склады:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveWarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            if (newWarehouseRow == null)
                return;

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

                    string query = @"INSERT INTO СКЛАД (Наименование, Адрес) VALUES (@name, @address)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@address", address);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Склад успешно добавлен!", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
                newWarehouseRow = null;
                SaveWarehouseButton.Visibility = Visibility.Collapsed;
                LoadWarehouses();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить склады:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
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
            SaveWarehouseButton.Visibility = Visibility.Visible;
        }

        private void OpenWarehouseDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = WarehousesDataGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show("Выберите склад.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Несохранённая (новая) строка — открывать нечего
            if (selectedRow["Id"] == DBNull.Value)
            {
                MessageBox.Show("Сначала сохраните склад, чтобы просмотреть остатки.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int warehouseId = Convert.ToInt32(selectedRow["Id"]);

            if (NavigationService != null)
                NavigationService.Navigate(new StockPage(warehouseId));
        }

        private void DeleteWarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView rowView = null;
            Button button = sender as Button;

            if (button != null) rowView = button.Tag as DataRowView;

            if (rowView == null) rowView = WarehousesDataGrid.SelectedItem as DataRowView;

            if (rowView == null)
            {
                MessageBox.Show("Выберите склад для удаления.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            object idValue = rowView["Id"];

            if (idValue == DBNull.Value)
            {
                if (MessageBox.Show("Удалить несохранённую запись?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                if (newWarehouseRow == rowView.Row)
                    newWarehouseRow = null;

                warehousesTable.Rows.Remove(rowView.Row);

                if (newWarehouseRow == null)
                    SaveWarehouseButton.Visibility = Visibility.Collapsed;

                return;
            }

            int warehouseId = Convert.ToInt32(idValue);

            if (MessageBox.Show("Вы действительно хотите удалить выбранный склад?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM склад WHERE id_склада = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", warehouseId);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Склад успешно удалён.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadWarehouses();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1451)
                {
                    MessageBox.Show( "Невозможно удалить склад: он используется в поставках, заказах или продажах.", "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Ошибка базы данных:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить склад:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (warehousesTable == null)
                return;

            string search = SearchTextBox.Text.Trim().Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");

            if (string.IsNullOrWhiteSpace(search))
            {
                warehousesTable.DefaultView.RowFilter = "";
                return;
            }
        }
    }
}