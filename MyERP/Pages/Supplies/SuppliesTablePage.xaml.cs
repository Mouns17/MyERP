using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MyERP.Pages.Supplies
{
    public partial class SuppliesTablePage : Page
    {
        private DataTable suppliesTable;

        public SuppliesTablePage()
        {
            InitializeComponent();
            LoadSupplies();
        }

        // Загрузка поставок
        private void LoadSupplies()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"SELECT p.id_поставки AS Id, ps.наименование AS Поставщик, p.дата_поставки AS Дата, s.наименование AS Склад, COALESCE( SUM(tp.количество * tp.закупочная_цена), 0) AS Сумма, st.название AS Статус, p.id_статуса AS StatusId FROM поставки p LEFT JOIN поставщики ps ON p.id_поставщика = ps.id_поставщика LEFT JOIN склад s ON p.id_склада = s.id_склада LEFT JOIN статус st ON p.id_статуса = st.id_статуса LEFT JOIN товар_поставка tp ON p.id_поставки = tp.id_поставки GROUP BY p.id_поставки, ps.наименование, p.дата_поставки, s.наименование, st.название, p.id_статуса ORDER BY p.id_поставки DESC;";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        suppliesTable = new DataTable();
                        adapter.Fill(suppliesTable);
                    }
                }

                SuppliesDataGrid.ItemsSource = suppliesTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки поставок:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppliesTable == null)
                return;

            string search = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(search))
            {
                suppliesTable.DefaultView.RowFilter = "";
                return;
            }

            search = search.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");

            suppliesTable.DefaultView.RowFilter ="Convert([Id], 'System.String') LIKE '%" + search + "%' OR " + "Convert([Поставщик], 'System.String') LIKE '%" + search + "%' OR " + "Convert([Склад], 'System.String') LIKE '%" + search + "%' OR " + "Convert([Статус], 'System.String') LIKE '%" + search + "%' OR " + "Convert([Дата], 'System.String') LIKE '%" + search + "%'";
        }

        private void OpenSupplyButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = SuppliesDataGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show("Выберите поставку.");
                return;
            }

            int supplyId = Convert.ToInt32(selectedRow["Id"]);
            NavigationService.Navigate(new SupplyDetailsPage(supplyId));
        }

        private void AddSuppliesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService navigation = NavigationService.GetNavigationService(this);

            if (navigation != null)
                navigation.Navigate(new CreateSupplyPage(UserSession.UserId));
        }

        private void DeleteSuppliesButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView selectedRow = SuppliesDataGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show("Выберите поставку для удаления.");
                return;
            }

            int supplyId = Convert.ToInt32(selectedRow["Id"]);
            string status = Convert.ToString(selectedRow["Статус"]);

            if (status != "Черновик")
            {
                MessageBox.Show("Удалять можно только поставки со статусом «Черновик».", "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show("Удалить выбранную поставку?\nВсе товарные строки документа также будут удалены.", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"DELETE FROM поставки WHERE id_поставки = @id AND id_статуса = ( SELECT id_статуса FROM статус WHERE название = 'Черновик' LIMIT 1);";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", supplyId);
                        int affectedRows = command.ExecuteNonQuery();

                        if (affectedRows == 0)
                        {
                            MessageBox.Show("Поставка не удалена. Возможно, её статус уже изменился.");
                            LoadSupplies();
                            return;
                        }
                    }
                }

                LoadSupplies();
                MessageBox.Show("Поставка успешно удалена.","Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления поставки:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}