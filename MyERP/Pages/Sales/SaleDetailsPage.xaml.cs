using MyERP.Classes;
using MySqlConnector;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages.Sales
{
    public partial class SaleDetailsPage : Page
    {
        private readonly int saleId;
        private int statusId;
        private ObservableCollection<SaleItem> saleItems;

        public SaleDetailsPage(int id)
        {
            InitializeComponent();

            saleId = id;
            saleItems = new ObservableCollection<SaleItem>();
            SaleItemsDataGrid.ItemsSource = saleItems;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    LoadCustomers(connection);
                    LoadWarehouses(connection);
                    LoadProducts(connection);

                    string query = "SELECT id_покупателя, id_склада, id_статуса, дата_продажи FROM продажи WHERE id_продажи = @id;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", saleId);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Продажа не найдена.");
                                Back();
                                return;
                            }

                            CustomerComboBox.SelectedValue = Convert.ToInt32(reader["id_покупателя"]);
                            WarehouseComboBox.SelectedValue = Convert.ToInt32(reader["id_склада"]);
                            statusId = Convert.ToInt32(reader["id_статуса"]);
                            SaleDatePicker.SelectedDate = Convert.ToDateTime(reader["дата_продажи"]);
                        }
                    }

                    LoadSaleItems(connection);
                }

                SetEditMode();
                UpdateTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продажи:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCustomers(MySqlConnection connection)
        {
            string query = "SELECT id_покупателя AS Id, наименование AS Наименование FROM покупатели ORDER BY наименование;";
            DataTable table = new DataTable();

            using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                adapter.Fill(table);

            CustomerComboBox.ItemsSource = table.DefaultView;
        }

        private void LoadWarehouses(MySqlConnection connection)
        {
            string query = "SELECT id_склада AS Id, наименование AS Наименование FROM склад ORDER BY наименование;";
            DataTable table = new DataTable();

            using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                adapter.Fill(table);

            WarehouseComboBox.ItemsSource = table.DefaultView;
        }

        private void LoadProducts(MySqlConnection connection)
        {
            string query = "SELECT id_товара AS Id, наименование AS Наименование, цена AS Цена FROM товар ORDER BY наименование;";
            DataTable table = new DataTable();

            using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                adapter.Fill(table);

            ProductComboBox.ItemsSource = table.DefaultView;
        }

        private void LoadSaleItems(MySqlConnection connection)
        {
            saleItems.Clear();

            string query = "SELECT tp.id_товара, t.наименование, tp.количество, tp.цена_продажи FROM товар_продажа tp INNER JOIN товар t ON t.id_товара = tp.id_товара WHERE tp.id_продажи = @id ORDER BY tp.id_товар_продажи;";

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", saleId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SaleItem item = new SaleItem();

                        item.Number = saleItems.Count + 1;
                        item.ProductId = Convert.ToInt32(reader["id_товара"]);
                        item.Name = Convert.ToString(reader["наименование"]);
                        item.Quantity = Convert.ToDecimal(reader["количество"]);
                        item.Price = Convert.ToDecimal(reader["цена_продажи"]);

                        saleItems.Add(item);
                    }
                }
            }
        }

        private void SetEditMode()
        {
            bool editable = statusId == 1;

            StatusTextBlock.Text = statusId == 1 ? "Черновик" : statusId == 2 ? "Проведен" : statusId == 3 ? "Отменен" : "Неизвестен";

            CustomerComboBox.IsEnabled = editable;
            WarehouseComboBox.IsEnabled = editable;
            SaleDatePicker.IsEnabled = editable;
            ProductComboBox.IsEnabled = editable;
            QuantityTextBox.IsEnabled = editable;
            PriceTextBox.IsEnabled = editable;
            SaleItemsDataGrid.IsEnabled = editable;
            SaveButton.IsEnabled = editable;
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView product = ProductComboBox.SelectedItem as DataRowView;

            if (product == null || PriceTextBox == null)
                return;

            PriceTextBox.Text = Convert.ToDecimal(product["Цена"]).ToString("0.00", CultureInfo.CurrentCulture);
        }

        private bool TryReadDecimal(string value, out decimal result)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result);
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView product = ProductComboBox.SelectedItem as DataRowView;

            if (product == null)
            {
                MessageBox.Show("Выберите товар.");
                return;
            }

            decimal quantity;
            decimal price;

            if (!TryReadDecimal(QuantityTextBox.Text, out quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество.");
                return;
            }

            if (!TryReadDecimal(PriceTextBox.Text, out price) || price < 0)
            {
                MessageBox.Show("Введите корректную цену.");
                return;
            }

            int productId = Convert.ToInt32(product["Id"]);

            foreach (SaleItem item in saleItems)
            {
                if (item.ProductId == productId)
                {
                    MessageBox.Show("Этот товар уже есть в продаже.");
                    return;
                }
            }

            SaleItem newItem = new SaleItem();

            newItem.Number = saleItems.Count + 1;
            newItem.ProductId = productId;
            newItem.Name = Convert.ToString(product["Наименование"]);
            newItem.Quantity = quantity;
            newItem.Price = price;

            saleItems.Add(newItem);

            ProductComboBox.SelectedIndex = -1;
            QuantityTextBox.Text = "1";
            PriceTextBox.Clear();

            UpdateTotal();
        }

        private void RemoveProductButton_Click(object sender, RoutedEventArgs e)
        {
            SaleItem item = SaleItemsDataGrid.SelectedItem as SaleItem;

            if (item == null)
            {
                MessageBox.Show("Выберите товар в таблице.");
                return;
            }

            saleItems.Remove(item);

            UpdateNumbers();
            UpdateTotal();
        }

        private void UpdateNumbers()
        {
            for (int i = 0; i < saleItems.Count; i++)
                saleItems[i].Number = i + 1;

            SaleItemsDataGrid.Items.Refresh();
        }

        private void UpdateTotal()
        {
            decimal total = 0m;

            foreach (SaleItem item in saleItems)
                total += item.Total;

            TotalTextBlock.Text = "ИТОГО: " + total.ToString("N2", CultureInfo.CurrentCulture) + " ₽";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (statusId != 1)
            {
                MessageBox.Show("Редактировать можно только черновики.");
                return;
            }

            if (CustomerComboBox.SelectedValue == null || WarehouseComboBox.SelectedValue == null || SaleDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Заполните покупателя, склад и дату.");
                return;
            }

            if (saleItems.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар.");
                return;
            }

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    using (MySqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string checkQuery = "SELECT id_статуса FROM продажи WHERE id_продажи = @id FOR UPDATE;";

                            using (MySqlCommand command = new MySqlCommand(checkQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@id", saleId);

                                object result = command.ExecuteScalar();

                                if (result == null || result == DBNull.Value || Convert.ToInt32(result) != 1)
                                    throw new Exception("Продажа больше не является черновиком.");
                            }

                            string updateQuery = "UPDATE продажи SET id_покупателя = @customer, id_склада = @warehouse, дата_продажи = @date WHERE id_продажи = @id;";

                            using (MySqlCommand command = new MySqlCommand(updateQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@customer", CustomerComboBox.SelectedValue);
                                command.Parameters.AddWithValue("@warehouse", WarehouseComboBox.SelectedValue);
                                command.Parameters.Add("@date", MySqlDbType.Date).Value = SaleDatePicker.SelectedDate.Value.Date;
                                command.Parameters.AddWithValue("@id", saleId);

                                command.ExecuteNonQuery();
                            }

                            string deleteQuery = "DELETE FROM товар_продажа WHERE id_продажи = @id;";

                            using (MySqlCommand command = new MySqlCommand(deleteQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@id", saleId);
                                command.ExecuteNonQuery();
                            }

                            string insertQuery = "INSERT INTO товар_продажа (id_продажи, id_товара, количество, цена_продажи) VALUES (@saleId, @productId, @quantity, @price);";

                            foreach (SaleItem item in saleItems)
                            {
                                using (MySqlCommand command = new MySqlCommand(insertQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@saleId", saleId);
                                    command.Parameters.AddWithValue("@productId", item.ProductId);
                                    command.Parameters.AddWithValue("@quantity", item.Quantity);
                                    command.Parameters.AddWithValue("@price", item.Price);

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

                MessageBox.Show("Изменения успешно сохранены.", "Продажи", MessageBoxButton.OK, MessageBoxImage.Information);
                Back();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back()
        {
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Back();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (statusId != 1)
            {
                MessageBox.Show("Провести можно только черновик.");
                return;
            }

            if (saleItems.Count == 0)
            {
                MessageBox.Show("Нельзя провести продажу без товаров.");
                return;
            }

            if (MessageBox.Show("Вы действительно хотите провести продажу?", "Проведение продажи", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    using (MySqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string checkQuery = "SELECT s.название FROM продажи p INNER JOIN статус s ON s.id_статуса = p.id_статуса WHERE p.id_продажи = @id FOR UPDATE;";

                            string currentStatus;

                            using (MySqlCommand command = new MySqlCommand(checkQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@id", saleId);

                                object result = command.ExecuteScalar();

                                if (result == null || result == DBNull.Value)
                                    throw new Exception("Продажа не найдена.");

                                currentStatus = Convert.ToString(result);
                            }

                            if (currentStatus != "Черновик")
                                throw new Exception("Провести можно только черновик.");

                            string statusQuery = "SELECT id_статуса FROM статус WHERE название = @name LIMIT 1;";
                            int newStatusId;

                            using (MySqlCommand command = new MySqlCommand(statusQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@name", "Проведен");

                                object result = command.ExecuteScalar();

                                if (result == null || result == DBNull.Value)
                                    throw new Exception("Статус «Проведен» не найден в базе данных.");

                                newStatusId = Convert.ToInt32(result);
                            }

                            string updateQuery = "UPDATE продажи SET id_статуса = @status WHERE id_продажи = @id;";

                            using (MySqlCommand command = new MySqlCommand(updateQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@status", newStatusId);
                                command.Parameters.AddWithValue("@id", saleId);
                                command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            statusId = newStatusId;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                SetEditMode();

                MessageBox.Show("Продажа успешно проведена.", "Продажи", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка изменения статуса:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}