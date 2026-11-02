using System.Windows;
using System.Windows.Input;

namespace MyERP.Windows
{
    public partial class MainWindow : Window
    {


        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new Pages.ProductsPage());
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

        private void Обзор_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Товары_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.ProductsPage());
        }

        private void Поставки_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.SuppliesPage());
        }

        private void Продажи_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Склады_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Логи_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Пользователи_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
