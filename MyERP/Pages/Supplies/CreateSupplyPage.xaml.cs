using MyERP.Classes;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages.Supplies
{
    public partial class CreateSupplyPage : Page
    {
        public ObservableCollection<ProductRow> Products { get; set; } = new ObservableCollection<ProductRow>();
        public List<Item> ProductsList { get; set; } = new List<Item>();

        public CreateSupplyPage(int userId) : this()
        {
           
        }

        public CreateSupplyPage()
        {
            InitializeComponent();
            DataContext = this;

            SupplyDatePicker.SelectedDate = DateTime.Today;

            ProductsDataGrid.ItemsSource = Products;
            LoadData();
            Products.CollectionChanged += (s, e) => RecalculateTotal();
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
                        string sql = @"INSERT INTO поставки (id_поставщика, id_склада, id_пользователя, id_статуса, дата_поставки) 
                                       VALUES (@sup, @wh, @user, @status, @date); SELECT LAST_INSERT_ID();";
                        long supplyId;
                        using (var cmd = new MySqlCommand(sql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@sup", SupplierComboBox.SelectedValue);
                            cmd.Parameters.AddWithValue("@wh", WarehouseComboBox.SelectedValue);
                            cmd.Parameters.AddWithValue("@user", UserSession.UserId);
                            cmd.Parameters.AddWithValue("@status", statusId);
                            cmd.Parameters.AddWithValue("@date", SupplyDatePicker.SelectedDate ?? DateTime.Today);
                            supplyId = Convert.ToInt64(cmd.ExecuteScalar());
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
                MessageBox.Show(statusId == 2 ? "Поставка успешно проведена! Остатки обновлены." : "Поставка сохранена как черновик.");
                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
    }
}