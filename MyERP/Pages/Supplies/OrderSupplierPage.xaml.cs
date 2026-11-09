using MyERP.Classes;
using MyERP.Pages.Supplies;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages.Supplies
{
    public partial class OrderSupplierPage : Page
    {
        private DataTable ordersTable;

        public OrderSupplierPage()
        {
            InitializeComponent();
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"SELECT z.id_заказа AS Id, p.наименование AS Поставщик, z.дата_заказа AS ДатаЗаказа, z.ожидаемая_дата_поставки AS ОжидаемаяДата, s.наименование AS Склад, COALESCE( SUM(tz.количество * tz.закупочная_цена), 0) AS Сумма, st.название AS Статус FROM заказы_поставщикам z LEFT JOIN поставщики p ON z.id_поставщика = p.id_поставщика LEFT JOIN склад s ON z.id_склада = s.id_склада LEFT JOIN статус st ON z.id_статуса = st.id_статуса LEFT JOIN товар_заказ tz ON z.id_заказа = tz.id_заказа GROUP BY z.id_заказа, p.наименование, z.дата_заказа, z.ожидаемая_дата_поставки, s.наименование, st.название ORDER BY z.id_заказа DESC;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        ordersTable = new DataTable();
                        adapter.Fill(ordersTable);

                        SupplierOrdersDataGrid.ItemsSource = ordersTable.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказов:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CreateOrderSupplierPage());
        }

        private void OpenOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (SupplierOrdersDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите заказ.");
                return;
            }

            DataRowView row = SupplierOrdersDataGrid.SelectedItem as DataRowView;

            if (row == null)
                return;

            int orderId = Convert.ToInt32(row["Id"]);

            NavigationService.Navigate(new CreateOrderSupplierPage(orderId));
        }

        private void CancelOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (SupplierOrdersDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите заказ.");
                return;
            }

            DataRowView row = SupplierOrdersDataGrid.SelectedItem as DataRowView;

            if (row == null)
                return;

            int orderId = Convert.ToInt32(row["Id"]);
            string status = Convert.ToString(row["Статус"]);

            if (status == "Отменён" || status == "Отменен")
            {
                MessageBox.Show("Заказ уже отменён.");
                return;
            }

            MessageBoxResult result = MessageBox.Show("Вы действительно хотите отменить заказ №" + orderId + "?", "Отмена заказа", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"UPDATE заказы_поставщикам SET id_статуса = (SELECT id_статуса FROM статус WHERE название = 'Отменен' LIMIT 1) WHERE id_заказа = @id;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", orderId);
                        int affected = command.ExecuteNonQuery();

                        if (affected == 0)
                        {
                            MessageBox.Show("Заказ не найден или статус не изменён.");
                            return;
                        }
                    }
                }

                MessageBox.Show("Заказ отменён.");
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка отмены заказа:\n" + ex.Message);
            }
        }

        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (ordersTable == null)
                return;

            string search = SearchTextBox.Text.Trim().Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");

            if (string.IsNullOrWhiteSpace(search))
            {
                ordersTable.DefaultView.RowFilter = "";
                return;
            }

            ordersTable.DefaultView.RowFilter = "Convert(Id, 'System.String') LIKE '%" + search + "%' OR " + "Поставщик LIKE '%" + search + "%' OR " + "Склад LIKE '%" + search + "%' OR " + "Статус LIKE '%" + search + "%'";
        }
    }
}
