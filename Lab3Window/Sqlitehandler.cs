
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;


namespace Lab3Window
{
    // Handles everything related to the SQLite database:
    // connecting, disconnecting, inserting keywords and reading them.
    // Form1 only calls these methods and doesn't write any SQL itself.
    public class SQLiteHandler
    {
        // Name of the database file (created next to the .exe)
        private readonly string dbFile;

        // The open connection. It is null while we are not connected.
        private SQLiteConnection conn;

        // Constructor: lets us choose the database file name (default "browser.db")
        public SQLiteHandler(string dbFile = "browser.db")
        {
            this.dbFile = dbFile;
        }

        // True when the connection exists and is open
        public bool IsConnected
        {
            get { return conn != null && conn.State == ConnectionState.Open; }
        }

        // Connects to the database.
        // If the file doesn't exist it is created, and the Keywords table is created
        // if it doesn't exist yet.
        public bool IsNewDatabase { get; private set; }
        public void ConnectToDb()
        {
            // Already connected: nothing to do
            if (IsConnected) return;

            // Creates an empty database file the first time
            IsNewDatabase = !File.Exists(dbFile);
            if (IsNewDatabase)
            {
                SQLiteConnection.CreateFile(dbFile);
            }

            conn = new SQLiteConnection("Data Source=" + dbFile);
            conn.Open();

            // Creates the table only if it doesn't exist (IF NOT EXISTS)
            string sql = @"CREATE TABLE IF NOT EXISTS Keywords (
                               id INTEGER PRIMARY KEY AUTOINCREMENT,
                               keyword TEXT)";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // Closes the connection to the database
        public void DisconnectFromDb()
        {
            if (conn != null)
            {
                conn.Close();
                conn.Dispose();
                conn = null;
            }
        }

        // Adds one keyword to the Keywords table
        public void InsertKeyword(string k)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to the database.");

            using (var cmd = new SQLiteCommand(
                "INSERT INTO Keywords (keyword) VALUES (@k)", conn))
            {
                // Parameter instead of putting the text directly in the SQL (safer)
                cmd.Parameters.AddWithValue("@k", k);
                cmd.ExecuteNonQuery();
            }
        }
        public void DeleteKeyword(string k)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to the database.");

            using (var cmd = new SQLiteCommand(
               "DELETE FROM Keywords WHERE keyword = @k", conn))
            {
                // Parameter instead of putting the text directly in the SQL (safer)
                cmd.Parameters.AddWithValue("@k", k);
                cmd.ExecuteNonQuery();
            }
        }

        // Returns all the keywords from the database as a list of strings
        public List<string> GetAllKeywords()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to the database.");

            var keywords = new List<string>();

            using (var cmd = new SQLiteCommand("SELECT keyword FROM Keywords ORDER BY id", conn))
            using (var reader = cmd.ExecuteReader())
            {
                // Read() moves to the next row; it returns false when there are no more rows
                while (reader.Read())
                {
                    keywords.Add(reader.GetString(0));
                }
            }

            return keywords;
        }
    }
}