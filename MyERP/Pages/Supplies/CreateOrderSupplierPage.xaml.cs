using MyERP.Classes;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages.Supplies
{
    public partial class CreateOrderSupplierPage : Page
    {
        public ObservableCollection<ProductRow> Products { get; set; } = new ObservableCollection<ProductRow>();
        public List<Item> ProductsList { get; set; } = new List<Item>();

        private int currentOrderId = 0; 

        public CreateOrderSupplierPage() : this(0) { }

        public CreateOrderSupplierPage(int orderId)
        {
            InitializeComponent();
            DataContext = this;
            currentOrderId = orderId;

            OrderDatePicker.SelectedDate = DateTime.Today;
            ExpectedDatePicker.SelectedDate = DateTime.Today.AddDays(7);

            ProductsDataGrid.ItemsSource = Products;
            Products.CollectionChanged += (s, e) => RecalculateTotal();

            LoadData(); 

            if (currentOrderId > 0)
            {
                LoadOrderForEdit(currentOrderId);
            }
        }

        private void LoadData()
        {
            try
            {
                using (var conn = DataBaseClass.GetConnection())
                {
                    conn.Open();

                    var dtSup = new DataTable();
                    new MySqlDataAdapter("SELECT id_поставщика AS Id, наименование AS Наименование FROM поставщики ORDER BY наименование", conn).Fill(dtSup);
                    SupplierComboBox.ItemsSource = dtSup.DefaultView;

                    var dtWh = new DataTable();
                    new MySqlDataAdapter("SELECT id_склада AS Id, наименование AS Наименование FROM склад ORDER BY наименование", conn).Fill(dtWh);
                    WarehouseComboBox.ItemsSource = dtWh.DefaultView;

                    var dtProd = new DataTable();
                    new MySqlDataAdapter("SELECT id_товара AS Id, наименование AS Наименование, цена AS Price FROM товар ORDER BY наименование", conn).Fill(dtProd);
                    foreach (DataRow row in dtProd.Rows)
                    {
                        ProductsList.Add(new Item
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            Наименование = row["Наименование"].ToString(),
                            Price = Convert.ToDecimal(row["Price"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников:\n" + ex.Message);
            }
        }

        private void LoadOrderForEdit(int orderId)
        {
            try
            {
                using (var conn = DataBaseClass.GetConnection())
                {
                    conn.Open();

                    string sqlOrder = @"SELECT id_поставщика, id_склада, дата_заказа, ожидаемая_дата_поставки, id_статуса 
                                        FROM заказы_поставщикам WHERE id_заказа = @id";
                    using (var cmd = new MySqlCommand(sqlOrder, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", orderId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                SupplierComboBox.SelectedValue = reader["id_поставщика"];
                                WarehouseComboBox.SelectedValue = reader["id_склада"];
                                OrderDatePicker.SelectedDate = Convert.ToDateTime(reader["дата_заказа"]);
                                ExpectedDatePicker.SelectedDate = reader["ожидаемая_дата_поставки"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["ожидаемая_дата_поставки"]) : (DateTime?)null;
                            }
                        }
                    }

                    string sqlItems = @"SELECT id_товара, количество, закупочная_цена 
                                        FROM товар_заказ WHERE id_заказа = @id ORDER BY id_товар_заказ";
                    using (var cmd = new MySqlCommand(sqlItems, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", orderId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            int rowNum = 1;
                            while (reader.Read())
                            {
                                Products.Add(new ProductRow
                                {
                                    RowNumber = rowNum++,
                                    ProductId = Convert.ToInt32(reader["id_товара"]),
                                    Quantity = Convert.ToDecimal(reader["количество"]),
                                    Price = Convert.ToDecimal(reader["закупочная_цена"])
                                });
                            }
                        }
                    }
                    RecalculateTotal();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказа:\n" + ex.Message);
            }
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            Products.Add(new ProductRow { RowNumber = Products.Count + 1 });
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is ProductRow row)
            {
                Products.Remove(row);
                for (int i = 0; i < Products.Count; i++) Products[i].RowNumber = i + 1;
                RecalculateTotal();
            }
        }

        private void Product_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is ProductRow row)
            {
                var item = ProductsList.FirstOrDefault(p => p.Id == row.ProductId);
                if (item != null && row.Price == 0) row.Price = item.Price;
            }
        }

        private void RecalculateTotal()
        {
            decimal total = Products.Sum(p => p.Total);
            TotalTextBlock.Text = total.ToString("N2") + " ₽";
        }

        private void SaveDraft_Click(object sender, RoutedEventArgs e) => SaveOrder(1); 
        private void CreateOrder_Click(object sender, RoutedEventArgs e) => SaveOrder(2); 

        private void SaveOrder(int statusId)
        {
            if (SupplierComboBox.SelectedValue == null || WarehouseComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите поставщика и склад.");
                return;
            }
            if (Products.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар.");
                return;
            }

            try
            {
                using (var conn = DataBaseClass.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        long orderId = currentOrderId;

                        if (currentOrderId == 0)
                        {
                            string sqlOrder = @"INSERT INTO заказы_поставщикам (id_поставщика, id_склада, id_пользователя, id_статуса, ожидаемая_дата_поставки) 
                                                VALUES (@sup, @wh, @user, @status, @expDate); SELECT LAST_INSERT_ID();";
                            using (var cmd = new MySqlCommand(sqlOrder, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@sup", SupplierComboBox.SelectedValue);
                                cmd.Parameters.AddWithValue("@wh", WarehouseComboBox.SelectedValue);
                                cmd.Parameters.AddWithValue("@user", UserSession.UserId);
                                cmd.Parameters.AddWithValue("@status", statusId);
                                cmd.Parameters.AddWithValue("@expDate", ExpectedDatePicker.SelectedDate);
                                orderId = Convert.ToInt64(cmd.ExecuteScalar());
                            }
                        }
                        else
                        {
                            string sqlUpdate = @"UPDATE заказы_поставщикам 
                                                 SET id_поставщика=@sup, id_склада=@wh, id_статуса=@status, ожидаемая_дата_поставки=@expDate 
                                                 WHERE id_заказа=@id";
                            using (var cmd = new MySqlCommand(sqlUpdate, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@sup", SupplierComboBox.SelectedValue);
                                cmd.Parameters.AddWithValue("@wh", WarehouseComboBox.SelectedValue);
                                cmd.Parameters.AddWithValue("@status", statusId);
                                cmd.Parameters.AddWithValue("@expDate", ExpectedDatePicker.SelectedDate);
                                cmd.Parameters.AddWithValue("@id", currentOrderId);
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = new MySqlCommand("DELETE FROM товар_заказ WHERE id_заказа=@id", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@id", currentOrderId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        foreach (var row in Products)
                        {
                            string sqlItem = @"INSERT INTO товар_заказ (id_заказа, id_товара, количество, закупочная_цена) 
                                               VALUES (@order, @prod, @qty, @price)";
                            using (var cmd = new MySqlCommand(sqlItem, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@order", orderId);
                                cmd.Parameters.AddWithValue("@prod", row.ProductId);
                                cmd.Parameters.AddWithValue("@qty", row.Quantity);
                                cmd.Parameters.AddWithValue("@price", row.Price);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                }
                MessageBox.Show(currentOrderId == 0 ? "Заказ успешно создан!" : "Заказ обновлён!");
                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
    }

    public class ProductRow : INotifyPropertyChanged
    {
        public int RowNumber { get; set; }

        private int _prodId;
        public int ProductId { get => _prodId; set { _prodId = value; Notify(); } }

        private decimal _qty;
        public decimal Quantity { get => _qty; set { _qty = value; Notify(); Notify(nameof(Total)); } }

        private decimal _price;
        public decimal Price { get => _price; set { _price = value; Notify(); Notify(nameof(Total)); } }

        public decimal Total => Quantity * Price;

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class Item
    {
        public int Id { get; set; }
        public string Наименование { get; set; }
        public decimal Price { get; set; }
    }
}