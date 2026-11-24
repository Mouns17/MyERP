using MyERP.Classes;
using MySqlConnector;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MyERP.Pages.Sales
{
    public partial class CreateSalePage : Page
    {
        private ObservableCollection<SaleItem> saleItems;
        private int currentSaleId = 0;
        private int currentStatusId = 0;

        public CreateSalePage() : this(0) { }

        public CreateSalePage(int saleId)
        {
            InitializeComponent();

            saleItems = new ObservableCollection<SaleItem>();
            SaleItemsDataGrid.ItemsSource = saleItems;
            SaleDatePicker.SelectedDate = DateTime.Today;

            currentSaleId = saleId;

            LoadDirectories();

            if (currentSaleId > 0)
            {
                TitleTextBlock.Text = $"ПРОДАЖА №{currentSaleId}";
                LoadSaleForEdit(currentSaleId);
            }
        }

        private void LoadDirectories()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    DataTable customers = new DataTable();
                    string customersQuery = "SELECT id_покупателя AS Id, наименование AS Наименование FROM покупатели ORDER BY наименование;";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(customersQuery, connection))
                        adapter.Fill(customers);
                    CustomerComboBox.ItemsSource = customers.DefaultView;

                    DataTable warehouses = new DataTable();
                    string warehousesQuery = "SELECT id_склада AS Id, наименование AS Наименование FROM склад ORDER BY наименование;";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(warehousesQuery, connection))
                        adapter.Fill(warehouses);
                    WarehouseComboBox.ItemsSource = warehouses.DefaultView;

                    DataTable products = new DataTable();
                    string productsQuery = "SELECT id_товара AS Id, наименование AS Наименование, цена AS Цена FROM товар ORDER BY наименование;";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(productsQuery, connection))
                        adapter.Fill(products);
                    ProductComboBox.ItemsSource = products.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSaleForEdit(int id)
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string statusName = "";
                    string query = @"SELECT p.id_покупателя, p.id_склада, p.id_статуса, p.дата_продажи, st.название AS status_name 
                                     FROM продажи p 
                                     JOIN статус st ON p.id_статуса = st.id_статуса 
                                     WHERE p.id_продажи = @id;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                CustomerComboBox.SelectedValue = Convert.ToInt32(reader["id_покупателя"]);
                                WarehouseComboBox.SelectedValue = Convert.ToInt32(reader["id_склада"]);
                                currentStatusId = Convert.ToInt32(reader["id_статуса"]);
                                SaleDatePicker.SelectedDate = Convert.ToDateTime(reader["дата_продажи"]);
                                statusName = reader["status_name"].ToString();
                            }
                            else
                            {
                                MessageBox.Show("Продажа не найдена.");
                                Back();
                                return;
                            }
                        }
                    }

                    string itemsQuery = @"SELECT tp.id_товара, t.наименование, tp.количество, tp.цена_продажи 
                                          FROM товар_продажа tp 
                                          JOIN товар t ON t.id_товара = tp.id_товара 
                                          WHERE tp.id_продажи = @id ORDER BY tp.id_товар_продажи;";

                    using (MySqlCommand command = new MySqlCommand(itemsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                SaleItem item = new SaleItem();
                                item.Number = saleItems.Count + 1;
                                item.ProductId = Convert.ToInt32(reader["id_товара"]);
                                item.Name = reader["наименование"].ToString();
                                item.Quantity = Convert.ToDecimal(reader["количество"]);
                                item.Price = Convert.ToDecimal(reader["цена_продажи"]);
                                saleItems.Add(item);
                            }
                        }
                    }

                    UpdateTotal();
                    ConfigureUIForStatus(statusName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных продажи:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigureUIForStatus(string statusName)
        {
            StatusTextBlock.Text = statusName;

            if (statusName == "Проведен")
                StatusTextBlock.Foreground = (Brush)new BrushConverter().ConvertFromString("#16A34A");
            else if (statusName == "Отменен")
                StatusTextBlock.Foreground = (Brush)new BrushConverter().ConvertFromString("#DC2626");

            if (statusName != "Черновик")
            {
                CustomerComboBox.IsEnabled = false;
                WarehouseComboBox.IsEnabled = false;
                SaleDatePicker.IsEnabled = false;
                ProductComboBox.IsEnabled = false;
                QuantityTextBox.IsEnabled = false;
                PriceTextBox.IsEnabled = false;
                SaleItemsDataGrid.IsReadOnly = true;

                BtnClear.Visibility = Visibility.Collapsed;
                BtnSave.Visibility = Visibility.Collapsed;
                BtnPost.Visibility = Visibility.Collapsed;
                BtnAdd.Visibility = Visibility.Collapsed;
                BtnRemove.Visibility = Visibility.Collapsed;
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView selectedProduct = ProductComboBox.SelectedItem as DataRowView;
            if (selectedProduct == null) return;
            PriceTextBox.Text = Convert.ToDecimal(selectedProduct["Цена"]).ToString("0.00", CultureInfo.CurrentCulture);
        }

        private bool TryReadDecimal(string value, out decimal result)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result);
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedProduct = ProductComboBox.SelectedItem as DataRowView;
            if (selectedProduct == null) { MessageBox.Show("Выберите товар."); return; }

            decimal quantity, price;
            if (!TryReadDecimal(QuantityTextBox.Text, out quantity) || quantity <= 0) { MessageBox.Show("Введите корректное количество товара."); return; }
            if (!TryReadDecimal(PriceTextBox.Text, out price) || price < 0) { MessageBox.Show("Введите корректную цену товара."); return; }

            int productId = Convert.ToInt32(selectedProduct["Id"]);

            foreach (SaleItem item in saleItems)
            {
                if (item.ProductId == productId) { MessageBox.Show("Этот товар уже добавлен в продажу."); return; }
            }

            SaleItem newItem = new SaleItem
            {
                Number = saleItems.Count + 1,
                ProductId = productId,
                Name = Convert.ToString(selectedProduct["Наименование"]),
                Quantity = quantity,
                Price = price
            };

            saleItems.Add(newItem);
            UpdateTotal();

            ProductComboBox.SelectedIndex = -1;
            QuantityTextBox.Text = "1";
            PriceTextBox.Clear();
        }

        private void RemoveProductButton_Click(object sender, RoutedEventArgs e)
        {
            SaleItem item = SaleItemsDataGrid.SelectedItem as SaleItem;
            if (item == null) { MessageBox.Show("Выберите товар в таблице."); return; }

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
            foreach (SaleItem item in saleItems) total += item.Total;
            TotalTextBlock.Text = "ИТОГО: " + total.ToString("N2", CultureInfo.CurrentCulture) + " ₽";
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить данные продажи?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            saleItems.Clear();
            CustomerComboBox.SelectedIndex = -1;
            WarehouseComboBox.SelectedIndex = -1;
            ProductComboBox.SelectedIndex = -1;
            SaleDatePicker.SelectedDate = DateTime.Today;
            QuantityTextBox.Text = "1";
            PriceTextBox.Clear();
            UpdateTotal();
        }

        private void SaveSaleButton_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerComboBox.SelectedValue == null) { MessageBox.Show("Выберите покупателя."); return; }
            if (WarehouseComboBox.SelectedValue == null) { MessageBox.Show("Выберите склад."); return; }
            if (SaleDatePicker.SelectedDate == null) { MessageBox.Show("Укажите дату продажи."); return; }
            if (saleItems.Count == 0) { MessageBox.Show("Добавьте хотя бы один товар."); return; }

            int customerId = Convert.ToInt32(CustomerComboBox.SelectedValue);
            int warehouseId = Convert.ToInt32(WarehouseComboBox.SelectedValue);
            int userId = UserSession.UserId;
            if (userId <= 0) { MessageBox.Show("Не удалось определить пользователя."); return; }

            DateTime saleDate = SaleDatePicker.SelectedDate.Value;

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();
                    using (MySqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            if (currentSaleId == 0)
                            {
                                string saleQuery = "INSERT INTO продажи (id_покупателя, id_склада, id_пользователя, id_статуса, дата_продажи) VALUES (@customerId, @warehouseId, @userId, @statusId, @saleDate);";
                                using (MySqlCommand command = new MySqlCommand(saleQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@customerId", customerId);
                                    command.Parameters.AddWithValue("@warehouseId", warehouseId);
                                    command.Parameters.AddWithValue("@userId", userId);
                                    command.Parameters.AddWithValue("@statusId", 1);
                                    command.Parameters.Add("@saleDate", MySqlDbType.Date).Value = saleDate.Date;
                                    command.ExecuteNonQuery();
                                }

                                using (MySqlCommand command = new MySqlCommand("SELECT LAST_INSERT_ID();", connection, transaction))
                                {
                                    currentSaleId = Convert.ToInt32(command.ExecuteScalar());
                                }
                            }
                            else
                            {
                                string updateQuery = "UPDATE продажи SET id_покупателя = @customerId, id_склада = @warehouseId, дата_продажи = @saleDate WHERE id_продажи = @id;";
                                using (MySqlCommand command = new MySqlCommand(updateQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@customerId", customerId);
                                    command.Parameters.AddWithValue("@warehouseId", warehouseId);
                                    command.Parameters.Add("@saleDate", MySqlDbType.Date).Value = saleDate.Date;
                                    command.Parameters.AddWithValue("@id", currentSaleId);
                                    command.ExecuteNonQuery();
                                }

                                using (MySqlCommand command = new MySqlCommand("DELETE FROM товар_продажа WHERE id_продажи = @id;", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@id", currentSaleId);
                                    command.ExecuteNonQuery();
                                }
                            }

                            string itemQuery = "INSERT INTO товар_продажа (id_продажи, id_товара, количество, цена_продажи) VALUES (@saleId, @productId, @quantity, @price);";
                            foreach (SaleItem item in saleItems)
                            {
                                using (MySqlCommand command = new MySqlCommand(itemQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@saleId", currentSaleId);
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

                MessageBox.Show("Продажа успешно сохранена.", "Продажи", MessageBoxButton.OK, MessageBoxImage.Information);
                Back();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения продажи:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PostSaleButton_Click(object sender, RoutedEventArgs e)
        {
            if (saleItems.Count == 0) { MessageBox.Show("Нельзя провести продажу без товаров."); return; }

            if (currentSaleId == 0)
            {
                MessageBox.Show("Сначала сохраните продажу как черновик.");
                return;
            }

            if (MessageBox.Show("Вы действительно хотите провести продажу?", "Проведение продажи", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();
                    string statusQuery = "SELECT id_статуса FROM статус WHERE название = 'Проведен' LIMIT 1;";
                    int newStatusId = 0;
                    using (MySqlCommand command = new MySqlCommand(statusQuery, connection))
                    {
                        object res = command.ExecuteScalar();
                        if (res == null) throw new Exception("Статус «Проведен» не найден.");
                        newStatusId = Convert.ToInt32(res);
                    }

                    string updateQuery = "UPDATE продажи SET id_статуса = @status WHERE id_продажи = @id;";
                    using (MySqlCommand command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@status", newStatusId);
                        command.Parameters.AddWithValue("@id", currentSaleId);
                        command.ExecuteNonQuery();
                    }
                    currentStatusId = newStatusId;
                }

                ConfigureUIForStatus("Проведен");
                MessageBox.Show("Продажа успешно проведена.", "Продажи", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка проведения продажи:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => Back();

        private void Back()
        {
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }

    public class SaleItem
    {
        public int Number { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
    }
}