using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages.Sales
{
    public partial class SalesListPage : Page
    {
        private DataTable salesTable;

        public SalesListPage()
        {
            InitializeComponent();
            LoadSales();
        }

        private void LoadSales()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"SELECT p.id_продажи AS Id, p.дата_продажи AS ДатаПродажи, pok.наименование AS Покупатель, s.наименование AS Склад, COUNT(DISTINCT tp.id_товар_продажи) AS КоличествоПозиций, COALESCE(SUM(tp.количество), 0) AS ОбщееКоличество, COALESCE(SUM(tp.количество * tp.цена_продажи), 0) AS Сумма, st.название AS Статус FROM продажи p INNER JOIN покупатели pok ON pok.id_покупателя = p.id_покупателя INNER JOIN склад s ON s.id_склада = p.id_склада INNER JOIN статус st ON st.id_статуса = p.id_статуса LEFT JOIN товар_продажа tp ON tp.id_продажи = p.id_продажи GROUP BY p.id_продажи, p.дата_продажи, pok.наименование, s.наименование, st.название ORDER BY p.id_продажи DESC;";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        salesTable = new DataTable();
                        adapter.Fill(salesTable);
                    }
                }

                if (!salesTable.Columns.Contains("ДатаДляПоиска"))
                    salesTable.Columns.Add("ДатаДляПоиска", typeof(string));

                foreach (DataRow row in salesTable.Rows)
                {
                    if (row["ДатаПродажи"] != DBNull.Value)
                    {
                        DateTime date = Convert.ToDateTime(row["ДатаПродажи"]);
                        row["ДатаДляПоиска"] = date.ToString("dd.MM.yyyy");
                    }
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продаж:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string EscapeFilterValue(string value)
        {
            return value.Replace("'", "''").Replace("]", "[]]").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
        }
        private void ApplyFilters()
        {
            if (salesTable == null || SearchTextBox == null)
                return;

            string search = SearchTextBox.Text.Trim();
            string filter = "";

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = EscapeFilterValue(search);

                filter = "Convert([Покупатель], 'System.String') LIKE '%" + search + "%' OR Convert([ДатаДляПоиска], 'System.String') LIKE '%" + search + "%'";
            }

            salesTable.DefaultView.RowFilter = filter;
            SalesDataGrid.ItemsSource = salesTable.DefaultView;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSales();
        }

        private void CreateSaleButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CreateSalePage());
        }

        private void OpenSaleButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = SalesDataGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show("Выберите продажу.");
                return;
            }

            int saleId = Convert.ToInt32(selectedRow["Id"]);
            NavigationService.Navigate(new SaleDetailsPage(saleId));
        }

        private void DeleteSaleButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            DataRowView selectedRow = null;

            if (button != null)
                selectedRow = button.Tag as DataRowView;

            if (selectedRow == null)
                selectedRow = SalesDataGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show("Выберите продажу для удаления.");
                return;
            }

            int saleId = Convert.ToInt32(selectedRow["Id"]);

            if (MessageBox.Show("Вы действительно хотите удалить продажу №" + saleId + "?\n\nВсе товарные позиции этой продажи также будут удалены.", "Удаление продажи", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = "DELETE FROM продажи WHERE id_продажи = @id;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", saleId);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Продажа успешно удалена.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadSales();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления продажи:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}