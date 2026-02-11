using Microsoft.Data.Sqlite;

namespace PacmanGame.Server.Services;

public class DatabaseService
{
    private const string ConnectionString = "Data Source=pacman.db";

    public DatabaseService()
    {
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS Rooms (
                Name TEXT PRIMARY KEY,
                Password TEXT
            );

            CREATE TABLE IF NOT EXISTS Players (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            );
        ";
        command.ExecuteNonQuery();
    }
}
