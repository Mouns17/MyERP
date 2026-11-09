using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages.Supplies
{
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

                    string query = @"SELECT id_поставщика AS Id, наименование AS Наименование, инн AS ИНН, телефон AS Телефон, электронная_почта AS ЭлПочта, адрес AS Адрес FROM поставщики ORDER BY id_поставщика DESC;";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        suppliersTable = new DataTable();
                        adapter.Fill(suppliersTable);
                    }
                }

                SuppliersDataGrid.ItemsSource = suppliersTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить поставщиков:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            DataRow row = suppliersTable.NewRow();

            row["Id"] = 0;
            row["Наименование"] = "";
            row["ИНН"] = DBNull.Value;
            row["Телефон"] = DBNull.Value;
            row["ЭлПочта"] = DBNull.Value;
            row["Адрес"] = DBNull.Value;

            suppliersTable.Rows.Add(row);
            SuppliersDataGrid.SelectedItem = suppliersTable.DefaultView[suppliersTable.DefaultView.Count - 1];
            SuppliersDataGrid.ScrollIntoView(SuppliersDataGrid.SelectedItem);
            SuppliersDataGrid.BeginEdit();
            SaveProductButton.Visibility = Visibility.Visible;
        }

        private void SaveSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            SuppliersDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            SuppliersDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    using (MySqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (DataRow row in suppliersTable.Rows)
                            {
                                if (row.RowState == DataRowState.Deleted)
                                    continue;

                                string name = Convert.ToString(row["Наименование"]).Trim();

                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    throw new Exception("Наименование поставщика не может быть пустым.");
                                }

                                int id = Convert.ToInt32(row["Id"]);

                                if (id == 0)
                                {
                                    string insertQuery = @"INSERT INTO поставщики (наименование, инн, телефон, электронная_почта, адрес) VALUES (@name, @inn, @phone, @email, @address);";

                                    using (MySqlCommand command = new MySqlCommand(insertQuery, connection, transaction))
                                    {
                                        AddSupplierParameters(command, row);
                                        command.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    string updateQuery = @"UPDATE поставщики SET наименование = @name, инн = @inn, телефон = @phone, электронная_почта = @email, адрес = @address WHERE id_поставщика = @id;";

                                    using (MySqlCommand command = new MySqlCommand(updateQuery, connection, transaction))
                                    {
                                        AddSupplierParameters(command, row);
                                        command.Parameters.AddWithValue("@id", id);
                                        command.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                MessageBox.Show("Данные поставщиков успешно сохранены.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadSuppliers();
                SaveProductButton.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddSupplierParameters(MySqlCommand command, DataRow row)
        {
            command.Parameters.AddWithValue("@name", Convert.ToString(row["Наименование"]).Trim());
            command.Parameters.AddWithValue("@inn", GetDatabaseValue(row["ИНН"]));
            command.Parameters.AddWithValue("@phone", GetDatabaseValue(row["Телефон"]));
            command.Parameters.AddWithValue("@email", GetDatabaseValue(row["ЭлПочта"]));
            command.Parameters.AddWithValue("@address", GetDatabaseValue(row["Адрес"]));
        }

        private object GetDatabaseValue(object value)
        {
            if (value == null || value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value)))
            {
                return DBNull.Value;
            }

            return value;
        }

        private void DeleteSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = SuppliersDataGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show("Выберите поставщика для удаления.");
                return;
            }

            int id = Convert.ToInt32(selectedRow["Id"]);

            if (id == 0)
            {
                selectedRow.Row.Delete();
                return;
            }

            MessageBoxResult result = MessageBox.Show("Вы действительно хотите удалить выбранного поставщика?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"DELETE FROM поставщики WHERE id_поставщика = @id;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                }

                LoadSuppliers();

                MessageBox.Show("Поставщик удалён.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1451)
                {
                    MessageBox.Show("Нельзя удалить поставщика, так как с ним связаны документы поставок.", "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Ошибка базы данных:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления:\n" + ex.Message);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppliersTable == null)
                return;

            string search = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(search))
            {
                suppliersTable.DefaultView.RowFilter = "";
                return;
            }

            search = search.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");

            suppliersTable.DefaultView.RowFilter = "Convert([Наименование], 'System.String') LIKE '%" + search + "%' OR " + "Convert([ИНН], 'System.String') LIKE '%" + search + "%' OR " + "Convert([Телефон], 'System.String') LIKE '%" + search + "%' OR " + "Convert([ЭлПочта], 'System.String') LIKE '%" + search + "%' OR " + "Convert([Адрес], 'System.String') LIKE '%" + search + "%'";
        }
    }
}