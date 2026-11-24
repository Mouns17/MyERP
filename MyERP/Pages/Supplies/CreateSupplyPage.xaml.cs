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
    public partial class CreateSupplyPage : Page
    {
        public ObservableCollection<ProductRow> Products { get; set; } = new ObservableCollection<ProductRow>();
        public List<Item> ProductsList { get; set; } = new List<Item>();

        private int currentSupplyId = 0;

        public CreateSupplyPage() : this(0) { }

        public CreateSupplyPage(int supplyId)
        {
            InitializeComponent();
            DataContext = this;
            currentSupplyId = supplyId;

            SupplyDatePicker.SelectedDate = DateTime.Today;
            ProductsDataGrid.ItemsSource = Products;
            Products.CollectionChanged += (s, e) => RecalculateTotal();

            LoadData();

            if (currentSupplyId > 0)
            {
                TitleTextBlock.Text = $"ПОСТАВКА №{currentSupplyId}";
                LoadSupplyForEdit(currentSupplyId);
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

        private void LoadSupplyForEdit(int id)
        {
            try
            {
                using (var conn = DataBaseClass.GetConnection())
                {
                    conn.Open();

                    string sqlSupply = @"SELECT id_поставщика, id_склада, дата_поставки, st.название AS status 
                                         FROM поставки p 
                                         JOIN статус st ON p.id_статуса = st.id_статуса 
                                         WHERE p.id_поставки = @id";
                    string statusName = "";
                    using (var cmd = new MySqlCommand(sqlSupply, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                SupplierComboBox.SelectedValue = reader["id_поставщика"];
                                WarehouseComboBox.SelectedValue = reader["id_склада"];
                                SupplyDatePicker.SelectedDate = Convert.ToDateTime(reader["дата_поставки"]);
                                statusName = reader["status"].ToString();
                            }
                        }
                    }

                    string sqlItems = @"SELECT id_товара, количество, закупочная_цена FROM товар_поставка WHERE id_поставки = @id";
                    using (var cmd = new MySqlCommand(sqlItems, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
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
                    ConfigureUIForStatus(statusName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки поставки:\n" + ex.Message);
            }
        }

        private void ConfigureUIForStatus(string statusName)
        {
            StatusTextBlock.Text = statusName;

            if (statusName == "Проведен")
            {
                StatusTextBlock.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#16A34A");
            }
            else if (statusName == "Отменен")
            {
                StatusTextBlock.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#DC2626");
            }

            if (statusName != "Черновик")
            {
                SupplierComboBox.IsEnabled = false;
                WarehouseComboBox.IsEnabled = false;
                SupplyDatePicker.IsEnabled = false;
                ProductsDataGrid.IsReadOnly = true;

                BtnAddProduct.Visibility = Visibility.Collapsed;
                BtnDeleteProduct.Visibility = Visibility.Collapsed;
                BtnSaveDraft.Visibility = Visibility.Collapsed;
                BtnPostSupply.Visibility = Visibility.Collapsed;
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
            TotalTextBlock.Text = Products.Sum(p => p.Total).ToString("N2") + " ₽";
        }

        private void SaveDraft_Click(object sender, RoutedEventArgs e) => SaveSupply(1);
        private void PostSupply_Click(object sender, RoutedEventArgs e) => SaveSupply(2);

        private void SaveSupply(int statusId)
        {
            if (SupplierComboBox.SelectedValue == null || WarehouseComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите поставщика и склад."); return;
            }
            if (Products.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар."); return;
            }

            try
            {
                using (var conn = DataBaseClass.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        long supplyId = currentSupplyId;

                        if (currentSupplyId == 0)
                        {
                            string sql = @"INSERT INTO поставки (id_поставщика, id_склада, id_пользователя, id_статуса, дата_поставки) 
                                           VALUES (@sup, @wh, @user, @status, @date); SELECT LAST_INSERT_ID();";
                            using (var cmd = new MySqlCommand(sql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@sup", SupplierComboBox.SelectedValue);
                                cmd.Parameters.AddWithValue("@wh", WarehouseComboBox.SelectedValue);
                                cmd.Parameters.AddWithValue("@user", UserSession.UserId);
                                cmd.Parameters.AddWithValue("@status", statusId);
                                cmd.Parameters.AddWithValue("@date", SupplyDatePicker.SelectedDate ?? DateTime.Today);
                                supplyId = Convert.ToInt64(cmd.ExecuteScalar());
                            }
                        }
                        else
                        {
                            string sqlUpdate = @"UPDATE поставки SET id_поставщика=@sup, id_склада=@wh, id_статуса=@status, дата_поставки=@date 
                                                 WHERE id_поставки=@id";
                            using (var cmd = new MySqlCommand(sqlUpdate, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@sup", SupplierComboBox.SelectedValue);
                                cmd.Parameters.AddWithValue("@wh", WarehouseComboBox.SelectedValue);
                                cmd.Parameters.AddWithValue("@status", statusId);
                                cmd.Parameters.AddWithValue("@date", SupplyDatePicker.SelectedDate ?? DateTime.Today);
                                cmd.Parameters.AddWithValue("@id", currentSupplyId);
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = new MySqlCommand("DELETE FROM товар_поставка WHERE id_поставки=@id", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@id", currentSupplyId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        foreach (var row in Products)
                        {
                            string sqlItem = @"INSERT INTO товар_поставка (id_поставки, id_товара, количество, закупочная_цена) 
                                               VALUES (@sup, @prod, @qty, @price)";
                            using (var cmd = new MySqlCommand(sqlItem, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@sup", supplyId);
                                cmd.Parameters.AddWithValue("@prod", row.ProductId);
                                cmd.Parameters.AddWithValue("@qty", row.Quantity);
                                cmd.Parameters.AddWithValue("@price", row.Price);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                }
                MessageBox.Show(statusId == 2 ? "Поставка успешно проведена! Остатки обновлены." : "Поставка сохранена.");
                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
    }

    public class ProductRow : INotifyPropertyChanged
    {
        public int RowNumber { get; set; }
        private int _prodId; public int ProductId { get => _prodId; set { _prodId = value; Notify(); } }
        private decimal _qty; public decimal Quantity { get => _qty; set { _qty = value; Notify(); Notify(nameof(Total)); } }
        private decimal _price; public decimal Price { get => _price; set { _price = value; Notify(); Notify(nameof(Total)); } }
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