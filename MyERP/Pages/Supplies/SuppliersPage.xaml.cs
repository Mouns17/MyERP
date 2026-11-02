using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;


namespace MyERP.Pages.Supplies
{
    /// <summary>
    /// Логика взаимодействия для SuppliersPage.xaml
    /// </summary>
    public partial class SuppliersPage : Page
    {
        private DataTable suppliersTable;
        private DataRow newSupplierRow;
        public SuppliersPage()
        {
            InitializeComponent();
            LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
                SELECT
                    pos.id_поставщика AS Id,
                    pos.Наименование AS Наименование,
                    pos.ИНН AS ИНН,
                    pos.Телефон AS Телефон,
                    pos.Электронная_почта AS ЭлПочта,
                    pos.Адрес AS Адрес
                FROM поставщики AS pos";
                    using (MySqlCommand command =
                           new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter =
                           new MySqlDataAdapter(command))
                    {
                        suppliersTable = new DataTable();
                        adapter.Fill(suppliersTable);

                        SuppliersDataGrid.ItemsSource =
                            suppliersTable.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить поставщиков:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            if (newSupplierRow == null)
                return;

            SuppliersDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            SuppliersDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            string name = newSupplierRow["Наименование"].ToString().Trim();
            string inn = newSupplierRow["ИНН"].ToString().Trim();
            string phone = newSupplierRow["Телефон"].ToString().Trim();
            string email = newSupplierRow["ЭлПочта"].ToString().Trim();
            string address = newSupplierRow["Адрес"].ToString().Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите наименование поставщика.");
                return;
            }

            if (string.IsNullOrWhiteSpace(inn))
            {
                MessageBox.Show("Введите инн поставщика.");
                return;
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Введите телефон поставщика.");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Введите эл. почту поставщика.");
                return;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("Введите адрес поставщика.");
                return;
            }

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
INSERT INTO ПОСТАВЩИКИ
(Наименование, ИНН, Телефон, Электронная_почта, адрес)
VALUES
(@name, @inn, @phone, @email, @address)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@inn", inn);
                        command.Parameters.AddWithValue("@phone", phone);
                        command.Parameters.AddWithValue("@email", email);
                        command.Parameters.AddWithValue("@address", address);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    "Поставщик успешно добавлен!",
                    "Сохранение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                newSupplierRow = null;

                SaveProductButton.Visibility = Visibility.Collapsed;

                LoadSuppliers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить поставщиков:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            if (newSupplierRow != null)
            {
                MessageBox.Show("Сначала сохраните текущего поставщика.");
                return;
            }

            newSupplierRow = suppliersTable.NewRow();

            newSupplierRow["Id"] = DBNull.Value;
            newSupplierRow["Наименование"] = "";
            newSupplierRow["ИНН"] = "";
            newSupplierRow["Телефон"] = "";
            newSupplierRow["ЭлПочта"] = "";
            newSupplierRow["Адрес"] = "";

            suppliersTable.Columns["Id"].AllowDBNull = true;

            suppliersTable.Rows.Add(newSupplierRow);

            SuppliersDataGrid.SelectedItem = newSupplierRow;
            SuppliersDataGrid.ScrollIntoView(newSupplierRow);

            SaveProductButton.Visibility = Visibility.Visible;
        }
    }
}
