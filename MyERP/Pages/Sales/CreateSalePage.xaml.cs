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
    public partial class CreateSalePage : Page
    {
        private ObservableCollection<SaleItem> saleItems;

        public CreateSalePage()
        {
            InitializeComponent();

            saleItems = new ObservableCollection<SaleItem>();
            SaleItemsDataGrid.ItemsSource = saleItems;
            SaleDatePicker.SelectedDate = DateTime.Today;

            LoadDirectories();
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
                    string productsQuery = "SELECT id_товара AS Id, наименование AS Наименование, артикул AS Артикул, цена AS Цена FROM товар ORDER BY наименование;";

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

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView selectedProduct = ProductComboBox.SelectedItem as DataRowView;

            if (selectedProduct == null)
                return;

            decimal price = Convert.ToDecimal(selectedProduct["Цена"]);
            PriceTextBox.Text = price.ToString("0.00", CultureInfo.CurrentCulture);
        }

        private bool TryReadDecimal(string value, out decimal result)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result);
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedProduct = ProductComboBox.SelectedItem as DataRowView;

            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите товар.");
                return;
            }

            decimal quantity;
            decimal price;

            if (!TryReadDecimal(QuantityTextBox.Text, out quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество товара.");
                return;
            }

            if (!TryReadDecimal(PriceTextBox.Text, out price) || price < 0)
            {
                MessageBox.Show("Введите корректную цену товара.");
                return;
            }

            int productId = Convert.ToInt32(selectedProduct["Id"]);

            foreach (SaleItem item in saleItems)
            {
                if (item.ProductId == productId)
                {
                    MessageBox.Show("Этот товар уже добавлен в продажу.");
                    return;
                }
            }

            SaleItem newItem = new SaleItem();

            newItem.Number = saleItems.Count + 1;
            newItem.ProductId = productId;
            newItem.Name = Convert.ToString(selectedProduct["Наименование"]);
            newItem.Quantity = quantity;
            newItem.Price = price;

            saleItems.Add(newItem);

            UpdateTotal();

            ProductComboBox.SelectedIndex = -1;
            QuantityTextBox.Text = "1";
            PriceTextBox.Clear();
        }

        private void RemoveProductButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
                return;

            SaleItem item = button.Tag as SaleItem;

            if (item == null)
                return;

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

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить данные продажи?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

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
            if (CustomerComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите покупателя.");
                return;
            }

            if (WarehouseComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите склад.");
                return;
            }

            if (SaleDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату продажи.");
                return;
            }

            if (saleItems.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар.");
                return;
            }

            int customerId = Convert.ToInt32(CustomerComboBox.SelectedValue);
            int warehouseId = Convert.ToInt32(WarehouseComboBox.SelectedValue);
            int userId = UserSession.UserId;

            if (userId <= 0)
            {
                MessageBox.Show("Не удалось определить пользователя. Выполните авторизацию.");
                return;
            }

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
                            int saleId;

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
                                saleId = Convert.ToInt32(command.ExecuteScalar());
                            }

                            string itemQuery = "INSERT INTO товар_продажа (id_продажи, id_товара, количество, цена_продажи) VALUES (@saleId, @productId, @quantity, @price);";

                            foreach (SaleItem item in saleItems)
                            {
                                using (MySqlCommand command = new MySqlCommand(itemQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@saleId", saleId);
                                    command.Parameters.AddWithValue("@productId", item.ProductId);
                                    command.Parameters.AddWithValue("@quantity", item.Quantity);
                                    command.Parameters.AddWithValue("@price", item.Price);

                                    command.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();

                            MessageBox.Show("Продажа №" + saleId + " успешно сохранена.\n\nСтатус: Черновик", "Продажи", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                if (NavigationService != null && NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения продажи:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
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

        public decimal Total
        {
            get { return Quantity * Price; }
        }
    }
}