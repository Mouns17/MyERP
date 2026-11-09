using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MyERP.Pages.Supplies
{
    public partial class SupplyDetailsPage : Page
    {
        private readonly int supplyId;

        public SupplyDetailsPage(int id)
        {
            InitializeComponent();

            supplyId = id;

            LoadSupplyDetails();
            LoadSupplyProducts();
        }

        private void LoadSupplyDetails()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"SELECT p.id_поставки, ps.наименование AS поставщик, s.наименование AS склад, p.дата_поставки, st.название AS статус FROM поставки p LEFT JOIN поставщики ps ON p.id_поставщика = ps.id_поставщика LEFT JOIN склад s ON p.id_склада = s.id_склада LEFT JOIN статус st ON p.id_статуса = st.id_статуса WHERE p.id_поставки = @id LIMIT 1;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", supplyId);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Поставка не найдена.");

                                if (NavigationService != null && NavigationService.CanGoBack)
                                {
                                    NavigationService.GoBack();
                                }

                                return;
                            }

                            SupplyTitleTextBlock.Text = "ПОСТАВКА №" + reader["id_поставки"].ToString();
                            SupplierTextBlock.Text = Convert.ToString(reader["поставщик"]);
                            WarehouseTextBlock.Text = Convert.ToString(reader["склад"]);
                            SupplyDateTextBlock.Text = Convert.ToDateTime(reader["дата_поставки"]).ToString("dd.MM.yyyy");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки информации о поставке:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSupplyProducts()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"SELECT ROW_NUMBER() OVER ( ORDER BY tp.id_товар_поставки ) AS Номер, t.наименование AS Товар, tp.количество AS Количество, tp.закупочная_цена AS Цена, tp.количество * tp.закупочная_цена AS Сумма FROM товар_поставка tp INNER JOIN товар t ON tp.id_товара = t.id_товара WHERE tp.id_поставки = @id ORDER BY tp.id_товар_поставки;";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@id", supplyId);

                        DataTable productsTable = new DataTable();
                        adapter.Fill(productsTable);

                        SupplyProductsDataGrid.ItemsSource = productsTable.DefaultView;

                        decimal total = 0;

                        foreach (DataRow row in productsTable.Rows)
                        {
                            if (row["Сумма"] != DBNull.Value)
                            {
                                total += Convert.ToDecimal(row["Сумма"]);
                            }
                        }

                        TotalAmountTextBlock.Text = total.ToString("N2") + " ₽";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки товаров поставки:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null &&
                NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection connection = DataBaseClass.GetConnection())
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int statusId;

                        using (MySqlCommand statusCommand = new MySqlCommand("SELECT id_статуса FROM статус WHERE название = @statusName LIMIT 1", connection, transaction))
                        {
                            statusCommand.Parameters.AddWithValue("@statusName", "Проведен");

                            object result = statusCommand.ExecuteScalar();

                            if (result == null)
                                throw new Exception("Статус 'Проведен' не найден.");

                            statusId = Convert.ToInt32(result);
                        }

                        // Обновляем статус поставки
                        using (MySqlCommand updateCommand = new MySqlCommand(@"UPDATE поставки SET id_статуса = @statusId WHERE id_поставки = @supplyId", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@statusId", statusId);
                            updateCommand.Parameters.AddWithValue("@supplyId", supplyId);

                            int affectedRows = updateCommand.ExecuteNonQuery();

                            if (affectedRows == 0)
                                throw new Exception("Поставка не найдена.");
                        }

                        transaction.Commit();

                        MessageBox.Show("Поставка успешно проведена!");

                        LoadSupplyDetails();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        MessageBox.Show("Ошибка проведения поставки: " + ex.Message);
                    }
                }
            }
        }
    }
}