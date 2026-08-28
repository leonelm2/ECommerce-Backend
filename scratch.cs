using System;
using Microsoft.Data.Sqlite;

class Program {
    static void Main() {
        try {
            using var connection = new SqliteConnection("Data Source=C:/Users/Docente/Desktop/ECommerce-Backend/src/ECommerce.Api/ecommerce.db");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(Orders);";
            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                Console.WriteLine(reader["name"]);
            }
        } catch (Exception e) {
            Console.WriteLine(e.Message);
        }
    }
}
