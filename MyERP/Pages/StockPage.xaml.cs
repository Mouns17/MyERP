using MyERP.Classes;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages
{
    public partial class StockPage : Page
    {
        private DataTable stockTable;
        private DataTable warehousesTable;
        private readonly int initialWarehouseId;

        public StockPage() : this(0) { }

        public StockPage(int warehouseId)
        {
            InitializeComponent();
            initialWarehouseId = warehouseId;
            LoadWarehouses();
            LoadStocks();
        }

        private void LoadWarehouses()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT id_склада AS Id, наименование AS Наименование
                                     FROM склад ORDER BY наименование";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        warehousesTable = new DataTable();
                        adapter.Fill(warehousesTable);
                    }
                }

                DataRow allRow = warehousesTable.NewRow();
                allRow["Id"] = 0;
                allRow["Наименование"] = "Все склады";
                warehousesTable.Rows.InsertAt(allRow, 0);

                WarehouseComboBox.ItemsSource = warehousesTable.DefaultView;
                WarehouseComboBox.SelectedIndex = 0;

                if (initialWarehouseId != 0)
                {
                    WarehouseComboBox.SelectedValue = initialWarehouseId;
                    if (WarehouseComboBox.SelectedIndex < 0)
                        WarehouseComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить склады:\n" + ex.Message,
                    "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStocks()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            ROW_NUMBER() OVER (ORDER BY s.наименование, t.наименование) AS Номер,
                            s.id_склада        AS СкладId,
                            t.артикул          AS Артикул,
                            t.наименование     AS Товар,
                            k.наименование     AS Категория,
                            s.наименование     AS Склад,
                            ei.наименование    AS Единица,
                            SUM(COALESCE(post.количество, 0)) - SUM(COALESCE(sale.количество, 0)) AS Остаток
                        FROM товар t
                        INNER JOIN категории k ON t.id_категории = k.id_категории
                        INNER JOIN единица_измерения ei ON t.id_единицы_измерения = ei.id_единицы_измерения
                        INNER JOIN склад s ON 1 = 1
                        LEFT JOIN (
                            SELECT tp.id_товара, p.id_склада, tp.количество
                            FROM товар_поставка tp
                            INNER JOIN поставки p ON tp.id_поставки = p.id_поставки
                            INNER JOIN статус st ON p.id_статуса = st.id_статуса
                            WHERE st.название = 'Проведен'
                        ) post ON post.id_товара = t.id_товара AND post.id_склада = s.id_склада
                        LEFT JOIN (
                            SELECT tpr.id_товара, pr.id_склада, tpr.количество
                            FROM товар_продажа tpr
                            INNER JOIN продажи pr ON tpr.id_продажи = pr.id_продажи
                            INNER JOIN статус st ON pr.id_статуса = st.id_статуса
                            WHERE st.название = 'Проведен'
                        ) sale ON sale.id_товара = t.id_товара AND sale.id_склада = s.id_склада
                        GROUP BY
                            s.id_склада, s.наименование,
                            t.id_товара, t.артикул, t.наименование, ei.наименование,
                            k.наименование
                        HAVING Остаток <> 0
                        ORDER BY s.наименование, t.наименование;";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        stockTable = new DataTable();
                        adapter.Fill(stockTable);
                    }
                }

                StockDataGrid.ItemsSource = stockTable.DefaultView;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки остатков:\n" + ex.Message,
                    "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (stockTable == null) return;

            string search = (SearchTextBox.Text ?? "").Trim();

            int warehouseId = 0;
            DataRowView selectedWarehouse = WarehouseComboBox.SelectedItem as DataRowView;
            if (selectedWarehouse != null)
                warehouseId = Convert.ToInt32(selectedWarehouse["Id"]);

            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search
                    .Replace("'", "''")
                    .Replace("[", "[[]")
                    .Replace("%", "[%]")
                    .Replace("*", "[*]");
                filters.Add("([Товар] LIKE '%" + s + "%' OR [Артикул] LIKE '%" + s + "%')");
            }

            if (warehouseId != 0)
                filters.Add("[СкладId] = " + warehouseId);

            stockTable.DefaultView.RowFilter =
                filters.Count > 0 ? string.Join(" AND ", filters) : "";

            TotalCountTextBlock.Text = stockTable.DefaultView.Count.ToString();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void WarehouseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            WarehouseComboBox.SelectedIndex = 0;
            LoadStocks();
        }
    }
}