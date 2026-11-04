
using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages.Supplies
{
    public partial class SuppliesTablePage : Page
    {
        private DataTable suppliesTable;
        private DataRow newSupplieRow;

        private DataTable suppliersTable;
        private DataTable warehousesTable;
        private DataTable productsTable;
        private DataTable statusesTable;

        public SuppliesTablePage()
        {
            InitializeComponent();

            LoadDirectories();
            LoadSupplies();
        }

        // Загрузка справочников
        private void LoadDirectories()
        {
            try
            {
                using (MySqlConnection connection =
                    DataBaseClass.GetConnection())
                {
                    connection.Open();

                    suppliersTable = LoadDirectory(
                        connection,
                        @"SELECT id_поставщика AS Id,
                                 Наименование
                          FROM поставщики
                          ORDER BY Наименование");

                    warehousesTable = LoadDirectory(
                        connection,
                        @"SELECT id_склада AS Id,
                                 Наименование
                          FROM склад
                          ORDER BY Наименование");

                    productsTable = LoadDirectory(
                        connection,
                        @"SELECT id_товара AS Id,
                                 Наименование
                          FROM товар
                          ORDER BY Наименование");

                    statusesTable = LoadDirectory(
                        connection,
                        @"SELECT id_статуса AS Id,
                                 Название AS Наименование
                          FROM статус
                          ORDER BY Название");
                }

                SupplierColumn.ItemsSource =
                    suppliersTable.DefaultView;

                WarehouseColumn.ItemsSource =
                    warehousesTable.DefaultView;

                ProductColumn.ItemsSource =
                    productsTable.DefaultView;

                StatusColumn.ItemsSource =
                    statusesTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки справочников:\n" + ex.Message);
            }
        }

        // Универсальная загрузка справочника
        private DataTable LoadDirectory(
            MySqlConnection connection,
            string query)
        {
            DataTable table = new DataTable();

            using (MySqlCommand command =
                new MySqlCommand(query, connection))
            using (MySqlDataAdapter adapter =
                new MySqlDataAdapter(command))
            {
                adapter.Fill(table);
            }

            return table;
        }

        // Загрузка поставок
        private void LoadSupplies()
        {
            try
            {
                using (MySqlConnection connection =
                    DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            p.id_поставки AS Id,
                            p.дата_поставки AS Дата,

                            p.id_поставщика AS id_поставщика,
                            pos.Наименование AS Поставщик,

                            p.id_склада AS id_склада,
                            s.Наименование AS Склад,

                            tp.id_товара AS id_товара,
                            t.Наименование AS Товар,

                            tp.Количество AS Количество,
                            tp.Закупочная_цена AS Цена,

                            p.id_статуса AS id_статуса,
                            st.Название AS Статус

                        FROM поставки AS p

                        JOIN поставщики AS pos
                            ON pos.id_поставщика = p.id_поставщика

                        JOIN склад AS s
                            ON s.id_склада = p.id_склада

                        JOIN статус AS st
                            ON st.id_статуса = p.id_статуса

                        JOIN товар_поставка AS tp
                            ON tp.id_поставки = p.id_поставки

                        JOIN товар AS t
                            ON t.id_товара = tp.id_товара

                        ORDER BY p.id_поставки DESC";

                    using (MySqlCommand command =
                        new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter =
                        new MySqlDataAdapter(command))
                    {
                        suppliesTable = new DataTable();
                        adapter.Fill(suppliesTable);

                        SuppliesDataGrid.ItemsSource =
                            suppliesTable.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить поставки:\n" + ex.Message);
            }
        }

        // Добавление новой строки
        private void AddSuppliesButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (newSupplieRow != null)
            {
                MessageBox.Show(
                    "Сначала сохраните текущую поставку.");
                return;
            }

            newSupplieRow = suppliesTable.NewRow();

            newSupplieRow["Id"] = DBNull.Value;

            newSupplieRow["Дата"] = DateTime.Today;
            newSupplieRow["id_поставщика"] = DBNull.Value;
            newSupplieRow["id_склада"] = DBNull.Value;
            newSupplieRow["id_товара"] = DBNull.Value;
            newSupplieRow["id_статуса"] = DBNull.Value;

            newSupplieRow["Поставщик"] = "";
            newSupplieRow["Склад"] = "";
            newSupplieRow["Товар"] = "";
            newSupplieRow["Количество"] = DBNull.Value;
            newSupplieRow["Цена"] = DBNull.Value;
            newSupplieRow["Статус"] = "";

            suppliesTable.Columns["Id"].AllowDBNull = true;

            suppliesTable.Rows.Add(newSupplieRow);

            SuppliesDataGrid.SelectedItem =
                suppliesTable.DefaultView[
                    suppliesTable.DefaultView.Count - 1];

            SuppliesDataGrid.ScrollIntoView(
                SuppliesDataGrid.SelectedItem);

            SaveSuppliesButton.Visibility = Visibility.Visible;
        }

        // Сохранение поставки
        private void SaveSuppliesButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (newSupplieRow == null)
                return;

            SuppliesDataGrid.CommitEdit(
                DataGridEditingUnit.Cell, true);

            SuppliesDataGrid.CommitEdit(
                DataGridEditingUnit.Row, true);

            // Проверка внешних ключей
            if (newSupplieRow.IsNull("id_поставщика"))
            {
                MessageBox.Show("Выберите поставщика.");
                return;
            }

            if (newSupplieRow.IsNull("id_склада"))
            {
                MessageBox.Show("Выберите склад.");
                return;
            }

            if (newSupplieRow.IsNull("id_товара"))
            {
                MessageBox.Show("Выберите товар.");
                return;
            }

            if (newSupplieRow.IsNull("id_статуса"))
            {
                MessageBox.Show("Выберите статус.");
                return;
            }

            // Получение ID
            int supplierId =
                Convert.ToInt32(newSupplieRow["id_поставщика"]);

            int warehouseId =
                Convert.ToInt32(newSupplieRow["id_склада"]);

            int productId =
                Convert.ToInt32(newSupplieRow["id_товара"]);

            int statusId =
                Convert.ToInt32(newSupplieRow["id_статуса"]);

            // Проверка количества
            decimal quantity;

            if (!decimal.TryParse(
                Convert.ToString(newSupplieRow["Количество"]),
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество.");
                return;
            }

            // Проверка цены
            decimal price;

            if (!decimal.TryParse(
                Convert.ToString(newSupplieRow["Цена"]),
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out price) || price < 0)
            {
                MessageBox.Show("Введите корректную закупочную цену.");
                return;
            }

            DateTime supplyDate;

            if (newSupplieRow.IsNull("Дата"))
            {
                MessageBox.Show("Выберите дату поставки.");
                return;
            }

            supplyDate = Convert.ToDateTime(newSupplieRow["Дата"]);

            try
            {
                using (MySqlConnection connection =
                    DataBaseClass.GetConnection())
                {
                    connection.Open();

                    using (MySqlTransaction transaction =
                        connection.BeginTransaction())
                    {
                        // Создание основной поставки
                        string querySupply = @"
    INSERT INTO поставки
(id_поставщика, id_склада, id_статуса,
 id_пользователя, дата_поставки)
VALUES
(@supplier, @warehouse, @status,
 @userId, @date)";

                        using (MySqlCommand command =
                            new MySqlCommand(
                                querySupply, connection, transaction))
                        {
                            command.Parameters.AddWithValue(
                                "@supplier", supplierId);

                            command.Parameters.AddWithValue(
                                "@date", supplyDate.Date);

                            command.Parameters.AddWithValue(
                                "@warehouse", warehouseId);

                            command.Parameters.AddWithValue(
                                "@status", statusId);

                            command.Parameters.AddWithValue(
                                "@userId", UserSession.UserId);

                            command.ExecuteNonQuery();
                        }

                        // Получение ID созданной поставки
                        long supplyId;

                        using (MySqlCommand command =
                            new MySqlCommand(
                                "SELECT LAST_INSERT_ID()",
                                connection, transaction))
                        {
                            supplyId = Convert.ToInt64(
                                command.ExecuteScalar());
                        }

                        // Добавление товара в поставку
                        string queryProduct = @"
                            INSERT INTO товар_поставка
                            (id_поставки, id_товара,
                             Количество, Закупочная_цена)
                            VALUES
                            (@supply, @product,
                             @quantity, @price)";

                        using (MySqlCommand command =
                            new MySqlCommand(
                                queryProduct, connection, transaction))
                        {
                            command.Parameters.AddWithValue(
                                "@supply", supplyId);

                            command.Parameters.AddWithValue(
                                "@product", productId);

                            command.Parameters.AddWithValue(
                                "@quantity", quantity);

                            command.Parameters.AddWithValue(
                                "@price", price);

                            command.ExecuteNonQuery();
                        }

                        // Подтверждение транзакции
                        transaction.Commit();
                    }
                }

                MessageBox.Show(
                    "Поставка успешно добавлена!",
                    "Сохранение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                newSupplieRow = null;

                SaveSuppliesButton.Visibility =
                    Visibility.Collapsed;

                LoadSupplies();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(
                    "Ошибка базы данных:\n" + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка сохранения:\n" + ex.Message);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Импорт будет реализован отдельно
        }

        private void DeleteSuppliesButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (suppliesTable == null)
                return;

            string search =
                SearchTextBox.Text.Trim()
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");

            if (string.IsNullOrWhiteSpace(search))
            {
                suppliesTable.DefaultView.RowFilter = "";
                return;
            }
        }
    }
}
