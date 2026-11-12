using MyERP.Classes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyERP.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainFrame.Navigate(new Pages.ProductsPage());

            Товары.Tag = "Active";

            UserRoleTextBlock.Text = UserSession.UserRole;

            if (UserSession.UserRole != "Администратор")
            {
                Пользователи.Visibility = Visibility.Collapsed;
            }
        }


        private void Товары_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTopNav(sender as Button);
            MainFrame.Navigate(new Pages.ProductsPage());
        }

        private void Поставки_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTopNav(sender as Button);
            MainFrame.Navigate(new Pages.SuppliesPage());
        }

        private void Продажи_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTopNav(sender as Button);
            MainFrame.Navigate(new Pages.SalesPage());
        }

        private void Пользователи_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTopNav(sender as Button);
            MainFrame.Navigate(new Pages.UsersPage());
        }
        private void SetActiveTopNav(Button clickedButton)
        {
            if (clickedButton == null) return;

            Товары.Tag = null;
            Поставки.Tag = null;
            Продажи.Tag = null;
            Пользователи.Tag = null;

            clickedButton.Tag = "Active";
        }


        private void WindowHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}