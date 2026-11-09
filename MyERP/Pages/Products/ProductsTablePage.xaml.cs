using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages.Products
{
    public partial class ProductsTablePage : Page
    {
        private DataTable productsTable;
        private DataRow newProductRow;

        public ProductsTablePage()
        {
            InitializeComponent();
            LoadProducts();
            LoadCategories();
            LoadUnits();
        }

        private void LoadProducts()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"SELECT t.id_товара AS Id, t.Артикул AS Артикул, t.Наименование AS Наименование, t.Цена AS Цена FROM ТОВАР AS t ORDER BY t.Наименование";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        productsTable = new DataTable();
                        adapter.Fill(productsTable);
                        ProductsDataGrid.ItemsSource = productsTable.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить товары:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (newProductRow == null)
                return;

            ProductsDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ProductsDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            string name = newProductRow["Наименование"].ToString().Trim();
            string article = newProductRow["Артикул"].ToString().Trim();

            decimal price;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите наименование товара.");
                return;
            }

            if (string.IsNullOrWhiteSpace(article))
            {
                MessageBox.Show("Введите артикул товара.");
                return;
            }

            if (!decimal.TryParse(newProductRow["Цена"].ToString(), out price) || price < 0)
            {
                MessageBox.Show("Введите корректную цену.");
                return;
            }

            if (CategoryComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите категорию товара.");
                return;
            }

            if (UnitComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите единицу измерения.");
                return;
            }

            int categoryId = Convert.ToInt32(CategoryComboBox.SelectedValue);
            int unitId = Convert.ToInt32(UnitComboBox.SelectedValue);

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"INSERT INTO ТОВАР (Артикул, Наименование, Цена, id_категории, id_единицы_измерения) VALUES (@article, @name, @price, @category, @unit)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@category", categoryId);
                        command.Parameters.AddWithValue("@unit", unitId);
                        command.Parameters.AddWithValue("@article", article);
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@price", price);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Товар успешно добавлен!", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);

                newProductRow = null;
                SaveProductButton.Visibility = Visibility.Collapsed;

                LoadProducts();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Товар с таким артикулом уже существует.");
                }
                else
                {
                    MessageBox.Show("Ошибка сохранения товара:\n" + ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка:\n" + ex.Message);
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (newProductRow != null)
            {
                MessageBox.Show("Сначала сохраните текущий товар.");
                return;
            }

            newProductRow = productsTable.NewRow();

            newProductRow["Id"] = DBNull.Value;
            newProductRow["Наименование"] = "";
            newProductRow["Цена"] = 0;
            newProductRow["Артикул"] = "";

            productsTable.Columns["Id"].AllowDBNull = true;

            productsTable.Rows.Add(newProductRow);

            ProductsDataGrid.SelectedItem = newProductRow;
            ProductsDataGrid.ScrollIntoView(newProductRow);

            SaveProductButton.Visibility = Visibility.Visible;
        }

        private void LoadCategories()
        {
            using (MySqlConnection connection = DataBaseClass.GetConnection())
            {
                connection.Open();

                string query = @"SELECT id_категории AS Id, Наименование FROM КАТЕГОРИИ";

                using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    CategoryComboBox.ItemsSource = table.DefaultView;
                }
            }
        }

        private void LoadUnits()
        {
            using (MySqlConnection connection = DataBaseClass.GetConnection())
            {
                connection.Open();

                string query = @"SELECT id_единицы_измерения AS Id, Наименование FROM ЕДИНИЦА_ИЗМЕРЕНИЯ";

                using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    UnitComboBox.ItemsSource = table.DefaultView;
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (productsTable == null)
                return;

            string search = SearchTextBox.Text.Trim().Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");

            if (string.IsNullOrWhiteSpace(search))
            {
                productsTable.DefaultView.RowFilter = "";
                return;
            }
        }

        private void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            DataRowView selectedRow = null;

            if (button != null)
                selectedRow = button.Tag as DataRowView;

            if (selectedRow == null)
                selectedRow = ProductsDataGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show("Выберите товар для удаления.");
                return;
            }

            if (selectedRow["Id"] == DBNull.Value)
            {
                DataRow row = selectedRow.Row;

                row.Delete();

                newProductRow = null;
                SaveProductButton.Visibility = Visibility.Collapsed;

                return;
            }

            int productId = Convert.ToInt32(selectedRow["Id"]);
            string productName = Convert.ToString(selectedRow["Наименование"]);

            if (MessageBox.Show("Вы действительно хотите удалить товар \"" + productName + "\"?", "Удаление товара", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"DELETE FROM товар WHERE id_товара = @id;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", productId);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Товар успешно удалён.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadProducts();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1451)
                {
                    MessageBox.Show("Невозможно удалить товар, поскольку он уже используется в поставках, заказах или продажах.", "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Ошибка удаления товара:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления товара:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}