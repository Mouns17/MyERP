using MySqlConnector;

namespace MyERP.Classes
{
    public static class DataBaseClass
    {
        private static string connStr =
            "server=localhost;database=erpdb;user=root;password=12345;SslMode=None";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connStr);
        }
    }
}