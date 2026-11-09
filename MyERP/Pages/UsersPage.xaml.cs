using MyERP.Classes;
using MySqlConnector;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MyERP.Pages
{
    public partial class UsersPage : Page
    {
        private DataTable usersTable;
        private DataTable rolesTable;
        public UsersPage()
        {
            InitializeComponent();
            LoadRoles();
            LoadUsers();
        }

        private void LoadRoles()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"SELECT id_роли AS Id, Название AS Название FROM Роль ORDER BY Название";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        rolesTable = new DataTable();
                        adapter.Fill(rolesTable);
                    }
                }

                UserRoleColumn.ItemsSource = rolesTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить роли:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUsers()
        {
            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"SELECT u.id_пользователя AS Id, u.Имя AS Имя, u.Логин AS Логин, u.Пароль AS Пароль, u.id_роли AS id_роли, r.Название AS Роль FROM ПОЛЬЗОВАТЕЛИ AS u LEFT JOIN Роль AS r ON u.id_роли = r.id_роли ORDER BY u.Имя";

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                    {
                        usersTable = new DataTable();
                        adapter.Fill(usersTable);
                    }
                }

                usersTable.Columns["Id"].AllowDBNull = true;

                UsersDataGrid.ItemsSource = usersTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить пользователей:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (usersTable == null)
                return;

            string search = SearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(search))
            {
                usersTable.DefaultView.RowFilter = "";
                return;
            }

            search = search.Replace("'", "''");
            search = search.Replace("[", "[[]");

            usersTable.DefaultView.RowFilter = "[Имя] LIKE '%" + search + "%' OR " + "[Логин] LIKE '%" + search + "%'";
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (usersTable == null || rolesTable == null)
                return;

            if (rolesTable.Rows.Count == 0)
            {
                MessageBox.Show("В базе данных отсутствуют роли.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (HasUnsavedUser())
            {
                MessageBox.Show("Сначала заполните и сохраните текущего пользователя.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DataRow newRow = usersTable.NewRow();
            newRow["Id"] = DBNull.Value;
            newRow["Имя"] = "";
            newRow["Логин"] = "";
            newRow["Пароль"] = "";
            newRow["id_роли"] = DBNull.Value;
            newRow["Роль"] = "";
            usersTable.Rows.Add(newRow);
            SearchTextBox.Text = "";
            UsersDataGrid.SelectedItem = usersTable.DefaultView[usersTable.DefaultView.Count - 1];
            UsersDataGrid.ScrollIntoView(UsersDataGrid.SelectedItem);
            UsersDataGrid.CurrentCell = new DataGridCellInfo(UsersDataGrid.SelectedItem, UsersDataGrid.Columns[1]);
            UsersDataGrid.BeginEdit();
            SaveUserButton.Visibility = Visibility.Visible;
        }

        private bool HasUnsavedUser()
        {
            foreach (DataRow row in usersTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;

                if (row["Id"] == DBNull.Value)
                    return true;
            }

            return false;
        }

        private void SaveUserButton_Click(object sender, RoutedEventArgs e)
        {
            UsersDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);

            UsersDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            if (usersTable == null)
                return;

            foreach (DataRow row in usersTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;

                if (row["Id"] != DBNull.Value)
                    continue;

                string name = Convert.ToString(row["Имя"]).Trim();

                string login = Convert.ToString(row["Логин"]).Trim();

                string password = Convert.ToString(row["Пароль"]).Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Введите имя пользователя.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(login))
                {
                    MessageBox.Show("Введите логин.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Введите пароль.");
                    return;
                }

                if (row["id_роли"] == DBNull.Value)
                {
                    MessageBox.Show("Выберите роль пользователя.");
                    return;
                }
            }

            MySqlTransaction transaction = null;

            try
            {
                using (MySqlConnection connection =
                    DataBaseClass.GetConnection())
                {
                    connection.Open();

                    transaction = connection.BeginTransaction();

                    string query = @"INSERT INTO ПОЛЬЗОВАТЕЛИ (Имя, Логин, Пароль, id_роли) VALUES (@name, @login, @password, @role)";

                    foreach (DataRow row in usersTable.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                            continue;

                        if (row["Id"] != DBNull.Value)
                            continue;

                        using (MySqlCommand command = new MySqlCommand( query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@name", Convert.ToString(row["Имя"]).Trim());
                            command.Parameters.AddWithValue("@login", Convert.ToString(row["Логин"]).Trim());
                            command.Parameters.AddWithValue("@password", Convert.ToString(row["Пароль"]).Trim());
                            command.Parameters.AddWithValue("@role", Convert.ToInt32(row["id_роли"]));
                            command.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }

                MessageBox.Show("Пользователи успешно добавлены!", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadUsers();
                SaveUserButton.Visibility = Visibility.Collapsed;
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
                MessageBox.Show("Не удалось сохранить пользователей:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            DataRowView selectedRow = null;

            if (button != null) selectedRow = button.Tag as DataRowView;

            if (selectedRow == null) selectedRow = UsersDataGrid.SelectedItem as DataRowView;

            if (selectedRow == null)
            {
                MessageBox.Show("Выберите пользователя для удаления.");
                return;
            }

            if (selectedRow["Id"] == DBNull.Value)
            {
                selectedRow.Row.Delete();
                SaveUserButton.Visibility = Visibility.Collapsed;
                return;
            }

            int userId = Convert.ToInt32(selectedRow["Id"]);

            string userName = Convert.ToString(selectedRow["Имя"]);

            if (userId == UserSession.UserId)
            {
                MessageBox.Show("Нельзя удалить пользователя, под которым выполнен вход.", "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Вы действительно хотите удалить пользователя \"" + userName + "\"?", "Удаление пользователя", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();
                    string query = @"DELETE FROM пользователи WHERE id_пользователя = @id;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", userId);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Пользователь успешно удалён.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadUsers();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1451)
                {
                    MessageBox.Show("Невозможно удалить пользователя, поскольку за ним " + "закреплены поставки, продажи или заказы.", "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Ошибка удаления пользователя:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления пользователя:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}