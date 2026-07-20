using Microsoft.Data.Sqlite;

var dbPath = @"C:\Users\Docente\Desktop\ECommerce-Backend-1\src\ECommerce.Api.Tests\bin\Debug\net8.0\ecommerce.db";
var connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();

using var connection = new SqliteConnection(connectionString);
connection.Open();

var tables = new[] { "Users", "Orders", "Products", "OrderItems" };

Console.WriteLine($"Inspecting database: {dbPath}");

foreach (var table in tables)
{
    Console.WriteLine($"\nTable: {table}");
    using var command = connection.CreateCommand();
    command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{table}';";
    var exists = command.ExecuteScalar();
    if (exists is null)
    {
        Console.WriteLine("  Table does not exist.");
        continue;
    }

    using var listCommand = connection.CreateCommand();
    listCommand.CommandText = $"SELECT * FROM {table} LIMIT 5;";
    using var reader = listCommand.ExecuteReader();
    var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
    Console.WriteLine("  Columns: " + string.Join(", ", columnNames));
    while (reader.Read())
    {
        var values = columnNames.Select(n => reader[n]?.ToString() ?? "NULL").ToArray();
        Console.WriteLine("  " + string.Join(" | ", values));
    }
}

using var userCountCommand = connection.CreateCommand();
userCountCommand.CommandText = "SELECT COUNT(*) FROM Users;";
var userCount = userCountCommand.ExecuteScalar();
Console.WriteLine($"\nTotal users: {userCount}");

using var adminCountCommand = connection.CreateCommand();
adminCountCommand.CommandText = "SELECT COUNT(*) FROM Users WHERE Role = 'Admin';";
var adminCount = adminCountCommand.ExecuteScalar();
Console.WriteLine($"Admin users: {adminCount}");

