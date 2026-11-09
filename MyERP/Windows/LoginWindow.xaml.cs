using MyERP.Classes;
using MySqlConnector;
using System;
using System.Windows;
using System.Windows.Input;

namespace MyERP.Windows
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            LoginTBox.Focus();
        }

        private void WindowHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeButton.Content = "□";
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeButton.Content = "❐";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoginTBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                PasswordTBox.Focus();
        }

        private void PasswordTBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                LoginButton_Click(sender, e);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTBox.Text.Trim();
            string password = PasswordTBox.Password;

            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Введите логин.", "Авторизация", MessageBoxButton.OK, MessageBoxImage.Warning);
                LoginTBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите пароль.", "Авторизация", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordTBox.Focus();
                return;
            }

            LoginButton.IsEnabled = false;

            try
            {
                using (MySqlConnection connection = DataBaseClass.GetConnection())
                {
                    connection.Open();

                    string query = @"SELECT p.id_пользователя, p.Логин, r.Название AS Название FROM пользователи AS p INNER JOIN роль AS r ON p.id_роли = r.id_роли WHERE p.Логин = @login AND p.Пароль = @password LIMIT 1;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Неверный логин или пароль.", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Warning);
                                PasswordTBox.Clear();
                                PasswordTBox.Focus();
                                return;
                            }

                            UserSession.UserId = Convert.ToInt32(reader["id_пользователя"]);
                            UserSession.Login = reader["Логин"].ToString();
                            UserSession.UserRole = reader["Название"].ToString();
                        }
                    }
                }

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Ошибка подключения к базе данных:\n" + ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка авторизации:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }
    }
}