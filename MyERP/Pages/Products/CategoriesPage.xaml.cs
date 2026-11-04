using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;


namespace MyERP.Pages.Products
{
    /// <summary>
    /// Логика взаимодействия для CategoriesPage.xaml
    /// </summary>
    public partial class CategoriesPage : Page
    {
        private DataTable categoriesTable;
        private DataRow newCategoryRow;
        public CategoriesPage()
        {
            InitializeComponent();
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
                    SELECT
                        c.id_категории AS Id,
                        c.Наименование AS Наименование
                    FROM КАТЕГОРИИ AS c
                    ORDER BY c.Наименование";

                    using (MySqlCommand command =
                           new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter =
                           new MySqlDataAdapter(command))
                    {
                        categoriesTable = new DataTable();
                        adapter.Fill(categoriesTable);

                        CategoriesDataGrid.ItemsSource =
                            categoriesTable.DefaultView;
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить категории:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (newCategoryRow == null)
                return;

            // Завершаем редактирование DataGrid
            CategoriesDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            CategoriesDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            string name = newCategoryRow["Наименование"].ToString().Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите наименование товара.");
                return;
            }

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"
INSERT INTO КАТЕГОРИИ
(Наименование)
VALUES
(@name)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    "Категория успешно добавлена!",
                    "Сохранение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                newCategoryRow = null;

                SaveCategoryButton.Visibility = Visibility.Collapsed;

                LoadCategories();

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить категории:\n" + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (newCategoryRow != null)
            {
                MessageBox.Show("Сначала сохраните текущую категорию .");
                return;
            }

            newCategoryRow = categoriesTable.NewRow();

            newCategoryRow["Id"] = DBNull.Value;
            newCategoryRow["Наименование"] = "";

            categoriesTable.Columns["Id"].AllowDBNull = true;

            categoriesTable.Rows.Add(newCategoryRow);

            CategoriesDataGrid.SelectedItem = newCategoryRow;
            CategoriesDataGrid.ScrollIntoView(newCategoryRow);

            SaveCategoryButton.Visibility = Visibility.Visible;
        }

        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (categoriesTable == null)
                return;

            string search =
                SearchTextBox.Text.Trim()
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");

            if (string.IsNullOrWhiteSpace(search))
            {
                categoriesTable.DefaultView.RowFilter = "";
                return;
            }
        }
    }
}
