using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;


namespace MyERP.Pages
{
    /// <summary>
    /// Логика взаимодействия для SuppliesPage.xaml
    /// </summary>
    public partial class SuppliesPage : Page
    {
        private DataTable suppliesTable;
        public SuppliesPage()
        {
            InitializeComponent();
            LoadSupplies();
        }

        private void LoadSupplies()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            p.id_поставки AS Id,
                            pos.Наименование AS Поставщик,
                            s.Наименование AS Склад,
                            t.Наименование AS Товар,
                            tp.Количество AS Количество,
                            tp.Закупочная_цена AS Цена,
                            st.Название AS Статус
                        FROM поставки AS p
                        JOIN поставщики AS pos ON pos.id_поставщика = p.id_поставщика
                        JOIN склад AS s ON s.id_склада = p.id_склада
                        JOIN статус AS st ON st.id_статуса = p.id_статуса
                        JOIN товар_поставка AS tp ON tp.id_поставки = p.id_поставки
                        JOIN товар AS t ON t.id_товара = tp.id_товара";
                    using (MySqlCommand command =
                           new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter =
                           new MySqlDataAdapter(command))
                    {
                        suppliesTable = new DataTable();
                        adapter.Fill(suppliesTable);

                        SuppliesDataGrid.ItemsSource =
                            suppliesTable.DefaultView;
                    }
                }
                }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить поставки:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}