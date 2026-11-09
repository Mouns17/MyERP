using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MyERP.Classes;
using MySqlConnector;

namespace MyERP.Pages.Supplies
{
    public partial class CreateSupplyPage : Page
    {
        private int userId;
        private DataTable suppliersTable;
        private DataTable warehousesTable;
        private DataTable productsCatalog;
        private DataTable supplyProductsTable;
        private bool isUpdatingRow = false;

        public CreateSupplyPage(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            SupplyDatePicker.SelectedDate = DateTime.Today;
            LoadSuppliers();
            LoadWarehouses();
            LoadProducts();
            CreateSupplyProductsTable();
            UpdateTotal();
        }

        private void LoadSuppliers()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = "SELECT id_поставщика AS Id, " + "наименование AS Наименование " + "FROM поставщики " + "ORDER BY наименование";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        suppliersTable = new DataTable();
                        adapter.Fill(suppliersTable);
                    }
                }

                SupplierComboBox.ItemsSource = suppliersTable.DefaultView;
                SupplierComboBox.DisplayMemberPath = "Наименование";
                SupplierComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки поставщиков:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadWarehouses()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = "SELECT id_склада AS Id, " + "наименование AS Наименование " + "FROM склад " + "ORDER BY наименование";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        warehousesTable = new DataTable();
                        adapter.Fill(warehousesTable);
                    }
                }

                WarehouseComboBox.ItemsSource = warehousesTable.DefaultView;
                WarehouseComboBox.DisplayMemberPath = "Наименование";
                WarehouseComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки складов:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = "SELECT id_товара AS Id, " + "наименование AS Наименование, " + "закупочная_цена AS ЦенаЗакупки " + "FROM товар " + "ORDER BY наименование";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        productsCatalog = new DataTable();
                        adapter.Fill(productsCatalog);
                    }
                }

                SupplyProductColumn.ItemsSource = productsCatalog.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show( "Ошибка загрузки товаров:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateSupplyProductsTable()
        {
            supplyProductsTable = new DataTable();

            DataColumn numberColumn = new DataColumn("Номер", typeof(int));
            DataColumn idColumn = new DataColumn("Id", typeof(int));
            DataColumn nameColumn = new DataColumn("Наименование", typeof(string));
            DataColumn quantityColumn = new DataColumn("Количество", typeof(decimal));
            DataColumn priceColumn = new DataColumn("Цена", typeof(decimal));
            DataColumn sumColumn = new DataColumn("Сумма", typeof(decimal));

            idColumn.AllowDBNull = true;

            supplyProductsTable.Columns.Add(numberColumn);
            supplyProductsTable.Columns.Add(idColumn);
            supplyProductsTable.Columns.Add(nameColumn);
            supplyProductsTable.Columns.Add(quantityColumn);
            supplyProductsTable.Columns.Add(priceColumn);
            supplyProductsTable.Columns.Add(sumColumn);
            supplyProductsTable.ColumnChanged += SupplyProductsTable_ColumnChanged;
            SupplyProductsDataGrid.ItemsSource = supplyProductsTable.DefaultView;
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            SupplyProductsDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            SupplyProductsDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            DataRow newRow = supplyProductsTable.NewRow();
            newRow["Номер"] = supplyProductsTable.Rows.Count + 1;
            newRow["Id"] = DBNull.Value;
            newRow["Наименование"] = "";
            newRow["Количество"] = 1m;
            newRow["Цена"] = 0m;
            newRow["Сумма"] = 0m;

            supplyProductsTable.Rows.Add(newRow);
            SupplyProductsDataGrid.SelectedItem = supplyProductsTable.DefaultView[supplyProductsTable.Rows.Count - 1];
            SupplyProductsDataGrid.ScrollIntoView(SupplyProductsDataGrid.SelectedItem);
            SupplyProductsDataGrid.CurrentCell = new DataGridCellInfo(SupplyProductsDataGrid.SelectedItem, SupplyProductColumn);
            SupplyProductsDataGrid.BeginEdit();
        }

        private void SupplyProductsTable_ColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            if (isUpdatingRow)
                return;

            try
            {
                isUpdatingRow = true;

                string columnName = e.Column.ColumnName;

                if (columnName == "Id")
                {
                    if (e.ProposedValue == DBNull.Value)
                    {
                        e.Row["Наименование"] = "";
                    }
                    else
                    {
                        int productId = Convert.ToInt32(e.ProposedValue);

                        DataRow[] foundProducts = productsCatalog.Select("Id = " + productId);

                        if (foundProducts.Length > 0)
                        {
                            DataRow product = foundProducts[0];

                            e.Row["Наименование"] = product["Наименование"];

                            e.Row["Цена"] = product["ЦенаЗакупки"];
                        }
                    }
                }

                if (columnName == "Количество" || columnName == "Цена" || columnName == "Id")
                {
                    CalculateRowSum(e.Row);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка изменения товара:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isUpdatingRow = false;
            }

            UpdateTotal();
        }

        private void CalculateRowSum(DataRow row)
        {
            decimal quantity = 0m;
            decimal price = 0m;

            if (row["Количество"] != DBNull.Value)
            {
                quantity = Convert.ToDecimal(row["Количество"]);
            }

            if (row["Цена"] != DBNull.Value)
            {
                price = Convert.ToDecimal(row["Цена"]);
            }

            row["Сумма"] = quantity * price;
        }

        private void UpdateTotal()
        {
            decimal total = 0m;

            if (supplyProductsTable != null)
            {
                foreach (DataRow row in supplyProductsTable.Rows)
                {
                    if (row.RowState == DataRowState.Deleted)
                        continue;

                    if (row["Сумма"] != DBNull.Value)
                    {
                        total += Convert.ToDecimal(row["Сумма"]);
                    }
                }
            }

            TotalAmountTextBlock.Text = total.ToString("N2") + " ₽";
        }

        private void RemoveProductButton_Click(object sender, RoutedEventArgs e)
        {
            SupplyProductsDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);

            SupplyProductsDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            DataRowView selectedRow = SupplyProductsDataGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show("Выберите товар для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            selectedRow.Row.Delete();
            for (int i = 0; i < supplyProductsTable.Rows.Count; i++)
            {
                supplyProductsTable.Rows[i]["Номер"] = i + 1;
            }

            UpdateTotal();
        }

        private void SaveSupplyButton_Click(object sender, RoutedEventArgs e)
        {
            SupplyProductsDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);

            SupplyProductsDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            if (SupplierComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите поставщика.");
                return;
            }
            if (WarehouseComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите склад.");
                return;
            }
            if (SupplyDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату поставки.");
                return;
            }
            if (supplyProductsTable.Rows.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар.");
                return;
            }
            foreach (DataRow row in supplyProductsTable.Rows)
            {
                if (row["Id"] == DBNull.Value)
                {
                    MessageBox.Show("Выберите товар во всех строках.");

                    return;
                }

                decimal quantity = Convert.ToDecimal(row["Количество"]);

                decimal price = Convert.ToDecimal(row["Цена"]);

                if (quantity <= 0)
                {
                    MessageBox.Show("Количество товара должно быть больше нуля.");

                    return;
                }

                if (price < 0)
                {
                    MessageBox.Show("Цена не может быть отрицательной.");

                    return;
                }
            }

            MySqlTransaction transaction = null;

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();
                    int statusId;
                    string statusQuery = "SELECT id_статуса " + "FROM статус " + "WHERE наименование = 'Черновик' " + "LIMIT 1";

                    using (MySqlCommand statusCommand = new MySqlCommand( statusQuery, connection, transaction))
                    {
                        object result = statusCommand.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                        {
                            throw new Exception("Статус «Черновик» не найден в базе данных.");
                        }

                        statusId = Convert.ToInt32(result);
                    }
                    string supplyQuery = "INSERT INTO поставки " + "(id_поставщика, id_склада, " + "id_пользователя, id_статуса, дата_поставки) " + "VALUES " + "(@supplier, @warehouse, @user, @status, @date)";

                    long supplyId;

                    using (MySqlCommand supplyCommand = new MySqlCommand( supplyQuery, connection, transaction))
                    {
                        supplyCommand.Parameters.AddWithValue("@supplier", Convert.ToInt32(SupplierComboBox.SelectedValue));
                        supplyCommand.Parameters.AddWithValue("@warehouse", Convert.ToInt32( WarehouseComboBox.SelectedValue));
                        supplyCommand.Parameters.AddWithValue("@user", userId);
                        supplyCommand.Parameters.AddWithValue("@status", statusId);
                        supplyCommand.Parameters.AddWithValue("@date", SupplyDatePicker.SelectedDate.Value);
                        supplyCommand.ExecuteNonQuery();
                    }
                    using (MySqlCommand idCommand = new MySqlCommand("SELECT LAST_INSERT_ID()", connection, transaction))
                    {
                        supplyId = Convert.ToInt64(idCommand.ExecuteScalar());
                    }

                    string productQuery = "INSERT INTO товар_поставка " + "(id_поставки, id_товара, " + "количество, закупочная_цена) " + "VALUES " + "(@supply, @product, @quantity, @price)";

                    foreach (DataRow row in supplyProductsTable.Rows)
                    {
                        using (MySqlCommand productCommand = new MySqlCommand( productQuery, connection, transaction))
                        {
                            productCommand.Parameters.AddWithValue("@supply", supplyId);
                            productCommand.Parameters.AddWithValue("@product", Convert.ToInt32(row["Id"]));
                            productCommand.Parameters.AddWithValue("@quantity", Convert.ToDecimal(row["Количество"]));
                            productCommand.Parameters.AddWithValue("@price", Convert.ToDecimal(row["Цена"]));
                            productCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }

                MessageBox.Show("Черновик поставки успешно сохранён!", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService != null &&
                    NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                    }
                }

                MessageBox.Show("Ошибка сохранения поставки:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}