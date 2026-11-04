using System;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MySqlConnector;
using MyERP.Classes;

namespace MyERP.Pages.Supplies.OrderSupplier
{
    public partial class CreateOrderSupplierPage : Page
    {
        private DataTable suppliersTable;
        private DataTable warehousesTable;
        private DataTable productsTable;
        private DataTable orderProductsTable;

        private int currentOrderId = 0;
        private bool isLoading = false;

        public CreateOrderSupplierPage()
        {
            InitializeComponent();

            InitializeOrderTable();
            LoadDirectories();

            OrderDatePicker.SelectedDate = DateTime.Today;
            ExpectedDatePicker.SelectedDate = DateTime.Today.AddDays(7);
        }

        // Конструктор для открытия существующего заказа
        public CreateOrderSupplierPage(int orderId)
            : this()
        {
            currentOrderId = orderId;
            LoadOrder(orderId);
        }

        // Создание таблицы товаров заказа
        private void InitializeOrderTable()
        {
            orderProductsTable = new DataTable();

            orderProductsTable.Columns.Add(
                "Номер", typeof(int));

            orderProductsTable.Columns.Add(
                "id_товара", typeof(int));

            orderProductsTable.Columns.Add(
                "Количество", typeof(decimal));

            orderProductsTable.Columns.Add(
                "Цена", typeof(decimal));

            orderProductsTable.Columns.Add(
                "Сумма", typeof(decimal));

            OrderProductsDataGrid.ItemsSource =
                orderProductsTable.DefaultView;
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

                    suppliersTable = new DataTable();
                    warehousesTable = new DataTable();
                    productsTable = new DataTable();

                    using (MySqlDataAdapter adapter =
                        new MySqlDataAdapter(
                            "SELECT id_поставщика AS Id, " +
                            "наименование AS Наименование " +
                            "FROM поставщики ORDER BY наименование",
                            connection))
                    {
                        adapter.Fill(suppliersTable);
                    }

                    using (MySqlDataAdapter adapter =
                        new MySqlDataAdapter(
                            "SELECT id_склада AS Id, " +
                            "наименование AS Наименование " +
                            "FROM склад ORDER BY наименование",
                            connection))
                    {
                        adapter.Fill(warehousesTable);
                    }

                    using (MySqlDataAdapter adapter =
                        new MySqlDataAdapter(
                            "SELECT id_товара AS Id, " +
                            "наименование AS Наименование, " +
                            "цена FROM товар ORDER BY наименование",
                            connection))
                    {
                        adapter.Fill(productsTable);
                    }
                }

                SupplierComboBox.ItemsSource =
                    suppliersTable.DefaultView;

                WarehouseComboBox.ItemsSource =
                    warehousesTable.DefaultView;

                OrderProductColumn.ItemsSource =
                    productsTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки справочников:\n" + ex.Message);
            }
        }

        // Добавление строки товара
        private void AddProductButton_Click(
            object sender, RoutedEventArgs e)
        {
            DataRow row = orderProductsTable.NewRow();

            row["Номер"] = orderProductsTable.Rows.Count + 1;
            row["id_товара"] = DBNull.Value;
            row["Количество"] = 1m;
            row["Цена"] = 0m;
            row["Сумма"] = 0m;

            orderProductsTable.Rows.Add(row);

            OrderProductsDataGrid.SelectedItem = row;
            OrderProductsDataGrid.ScrollIntoView(row);
        }

        // Удаление выбранного товара
        private void RemoveProductButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (OrderProductsDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар для удаления.");
                return;
            }

            DataRowView row =
                OrderProductsDataGrid.SelectedItem as DataRowView;

            if (row == null)
                return;

            row.Row.Delete();

            RenumberProducts();
            CalculateTotal();
        }

        // Перенумерация товаров
        private void RenumberProducts()
        {
            int number = 1;

            foreach (DataRow row in orderProductsTable.Rows)
            {
                if (row.RowState != DataRowState.Deleted)
                {
                    row["Номер"] = number;
                    number++;
                }
            }
        }

        // Пересчёт суммы заказа
        private void CalculateTotal()
        {
            decimal total = 0m;

            foreach (DataRow row in orderProductsTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;

                decimal quantity = 0m;
                decimal price = 0m;

                if (row["Количество"] != DBNull.Value)
                    quantity = Convert.ToDecimal(row["Количество"]);

                if (row["Цена"] != DBNull.Value)
                    price = Convert.ToDecimal(row["Цена"]);

                decimal sum = quantity * price;

                row["Сумма"] = sum;

                total += sum;
            }

            TotalAmountTextBlock.Text =
                total.ToString("N2") + " ₽";
        }

        // Пересчёт после изменения ячейки
        private void OrderProductsDataGrid_CellEditEnding(
            object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(
                new Action(delegate
                {
                    CalculateTotal();
                }),
                System.Windows.Threading.DispatcherPriority.Background);
        }

        // Открытие существующего заказа
        private void LoadOrder(int orderId)
        {
            try
            {
                isLoading = true;

                using (MySqlConnection connection =
                    DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string headerQuery = @"
                        SELECT
                            id_поставщика,
                            id_склада,
                            дата_заказа,
                            ожидаемая_дата_поставки,
                            id_статуса
                        FROM заказы_поставщикам
                        WHERE id_заказа = @id;";

                    using (MySqlCommand command =
                        new MySqlCommand(headerQuery, connection))
                    {
                        command.Parameters.AddWithValue("@id", orderId);

                        using (MySqlDataReader reader =
                            command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Заказ не найден.");
                                NavigationService.GoBack();
                                return;
                            }

                            SupplierComboBox.SelectedValue =
                                Convert.ToInt32(reader["id_поставщика"]);

                            WarehouseComboBox.SelectedValue =
                                Convert.ToInt32(reader["id_склада"]);

                            OrderDatePicker.SelectedDate =
                                Convert.ToDateTime(reader["дата_заказа"]);

                            if (reader["ожидаемая_дата_поставки"] != DBNull.Value)
                            {
                                ExpectedDatePicker.SelectedDate =
                                    Convert.ToDateTime(
                                        reader["ожидаемая_дата_поставки"]);
                            }
                        }
                    }

                    string productsQuery = @"
                        SELECT
                            tz.id_товара,
                            tz.количество,
                            tz.закупочная_цена
                        FROM товар_заказ tz
                        WHERE tz.id_заказа = @id;";

                    using (MySqlCommand command =
                        new MySqlCommand(productsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@id", orderId);

                        using (MySqlDataReader reader =
                            command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DataRow row =
                                    orderProductsTable.NewRow();

                                row["Номер"] =
                                    orderProductsTable.Rows.Count + 1;

                                row["id_товара"] =
                                    Convert.ToInt32(reader["id_товара"]);

                                row["Количество"] =
                                    Convert.ToDecimal(reader["количество"]);

                                row["Цена"] =
                                    Convert.ToDecimal(
                                        reader["закупочная_цена"]);

                                row["Сумма"] =
                                    Convert.ToDecimal(reader["количество"]) *
                                    Convert.ToDecimal(
                                        reader["закупочная_цена"]);

                                orderProductsTable.Rows.Add(row);
                            }
                        }
                    }
                }

                CalculateTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки заказа:\n" + ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        // Сохранение черновика
        private void SaveDraftButton_Click(
            object sender, RoutedEventArgs e)
        {
            SaveOrder("Черновик");
        }

        // Проведение заказа
        private void SaveOrderButton_Click(
            object sender, RoutedEventArgs e)
        {
            SaveOrder("Проведен");
        }

        // Основное сохранение заказа
        private void SaveOrder(string statusName)
        {
            if (isLoading)
                return;

            // Завершаем редактирование текущей ячейки
            OrderProductsDataGrid.CommitEdit(
                DataGridEditingUnit.Cell, true);

            OrderProductsDataGrid.CommitEdit(
                DataGridEditingUnit.Row, true);

            if (SupplierComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите поставщика.");
                return;
            }

            if (WarehouseComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите склад.");
                return;
            }

            if (OrderDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату заказа.");
                return;
            }

            if (ExpectedDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите ожидаемую дату поставки.");
                return;
            }

            if (ExpectedDatePicker.SelectedDate.Value.Date <
                OrderDatePicker.SelectedDate.Value.Date)
            {
                MessageBox.Show(
                    "Ожидаемая дата не может быть раньше даты заказа.");
                return;
            }

            if (orderProductsTable.Rows.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар.");
                return;
            }

            decimal total = 0m;

            foreach (DataRow row in orderProductsTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;

                if (row["id_товара"] == DBNull.Value)
                {
                    MessageBox.Show("Выберите товар во всех строках.");
                    return;
                }

                decimal quantity;
                decimal price;

                if (!decimal.TryParse(
                    Convert.ToString(row["Количество"]),
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out quantity) || quantity <= 0)
                {
                    MessageBox.Show(
                        "Количество должно быть больше нуля.");
                    return;
                }

                if (!decimal.TryParse(
                    Convert.ToString(row["Цена"]),
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out price) || price < 0)
                {
                    MessageBox.Show("Проверьте закупочную цену.");
                    return;
                }

                total += quantity * price;
            }

            if (statusName == "Проведен")
            {
                MessageBoxResult confirmation = MessageBox.Show(
                    "Провести заказ? После проведения его состав " +
                    "не следует изменять.",
                    "Проведение заказа",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes)
                    return;
            }

            try
            {
                using (MySqlConnection connection =
                    DataBaseClass.GetConnection())
                {
                    connection.Open();

                    using (MySqlTransaction transaction =
                        connection.BeginTransaction())
                    {
                        try
                        {
                            int statusId = GetStatusId(
                                connection, transaction, statusName);

                            if (statusId == 0)
                                throw new Exception(
                                    "Статус '" + statusName +
                                    "' не найден в таблице статус.");

                            if (currentOrderId == 0)
                            {
                                string insertHeader = @"
                                    INSERT INTO заказы_поставщикам
                                    (
                                        id_поставщика,
                                        id_склада,
                                        id_пользователя,
                                        id_статуса,
                                        дата_заказа,
                                        ожидаемая_дата_поставки
                                    )
                                    VALUES
                                    (
                                        @supplier,
                                        @warehouse,
                                        @user,
                                        @status,
                                        @orderDate,
                                        @expectedDate
                                    );

                                    SELECT LAST_INSERT_ID();";

                                using (MySqlCommand command =
                                    new MySqlCommand(
                                        insertHeader,
                                        connection,
                                        transaction))
                                {
                                    command.Parameters.AddWithValue(
                                        "@supplier",
                                        SupplierComboBox.SelectedValue);

                                    command.Parameters.AddWithValue(
                                        "@warehouse",
                                        WarehouseComboBox.SelectedValue);

                                    command.Parameters.AddWithValue(
                                        "@user",
                                        UserSession.UserId);

                                    command.Parameters.AddWithValue(
                                        "@status", statusId);

                                    command.Parameters.AddWithValue(
                                        "@orderDate",
                                        OrderDatePicker.SelectedDate.Value);

                                    command.Parameters.AddWithValue(
                                        "@expectedDate",
                                        ExpectedDatePicker.SelectedDate.Value.Date);

                                    currentOrderId = Convert.ToInt32(
                                        command.ExecuteScalar());
                                }
                            }
                            else
                            {
                                string updateHeader = @"
                                    UPDATE заказы_поставщикам
                                    SET
                                        id_поставщика = @supplier,
                                        id_склада = @warehouse,
                                        id_статуса = @status,
                                        дата_заказа = @orderDate,
                                        ожидаемая_дата_поставки = @expectedDate
                                    WHERE id_заказа = @id;";

                                using (MySqlCommand command =
                                    new MySqlCommand(
                                        updateHeader,
                                        connection,
                                        transaction))
                                {
                                    command.Parameters.AddWithValue(
                                        "@supplier",
                                        SupplierComboBox.SelectedValue);

                                    command.Parameters.AddWithValue(
                                        "@warehouse",
                                        WarehouseComboBox.SelectedValue);

                                    command.Parameters.AddWithValue(
                                        "@status", statusId);

                                    command.Parameters.AddWithValue(
                                        "@orderDate",
                                        OrderDatePicker.SelectedDate.Value);

                                    command.Parameters.AddWithValue(
                                        "@expectedDate",
                                        ExpectedDatePicker.SelectedDate.Value.Date);

                                    command.Parameters.AddWithValue(
                                        "@id", currentOrderId);

                                    command.ExecuteNonQuery();
                                }

                                // Заменяем прежний состав заказа
                                string deleteProducts = @"
                                    DELETE FROM товар_заказ
                                    WHERE id_заказа = @id;";

                                using (MySqlCommand command =
                                    new MySqlCommand(
                                        deleteProducts,
                                        connection,
                                        transaction))
                                {
                                    command.Parameters.AddWithValue(
                                        "@id", currentOrderId);

                                    command.ExecuteNonQuery();
                                }
                            }

                            // Добавляем позиции заказа
                            string insertProduct = @"
                                INSERT INTO товар_заказ
                                (
                                    id_заказа,
                                    id_товара,
                                    количество,
                                    закупочная_цена
                                )
                                VALUES
                                (
                                    @orderId,
                                    @productId,
                                    @quantity,
                                    @price
                                );";

                            foreach (DataRow row in orderProductsTable.Rows)
                            {
                                if (row.RowState == DataRowState.Deleted)
                                    continue;

                                using (MySqlCommand command =
                                    new MySqlCommand(
                                        insertProduct,
                                        connection,
                                        transaction))
                                {
                                    command.Parameters.AddWithValue(
                                        "@orderId", currentOrderId);

                                    command.Parameters.AddWithValue(
                                        "@productId", row["id_товара"]);

                                    command.Parameters.AddWithValue(
                                        "@quantity",
                                        Convert.ToDecimal(row["Количество"]));

                                    command.Parameters.AddWithValue(
                                        "@price",
                                        Convert.ToDecimal(row["Цена"]));

                                    command.ExecuteNonQuery();
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

                MessageBox.Show(
                    "Заказ №" + currentOrderId +
                    " успешно сохранён.\nСтатус: " + statusName,
                    "Сохранение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (NavigationService != null &&
                    NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка сохранения заказа:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Получение ID статуса по названию
        private int GetStatusId(
            MySqlConnection connection,
            MySqlTransaction transaction,
            string statusName)
        {
            string query = @"
                SELECT id_статуса
                FROM статус
                WHERE название = @name
                LIMIT 1;";

            using (MySqlCommand command =
                new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@name", statusName);

                object result = command.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return 0;

                return Convert.ToInt32(result);
            }
        }

        // Возврат к списку заказов
        private void BackButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (NavigationService != null &&
                NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}
