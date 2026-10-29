
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;


namespace Lab3Window
{
    // Handles everything related to the MSSQL database:
    // connecting, disconnecting, inserting keywords and reading them.
    // Form1 only calls these methods and doesn't write any SQL itself.
    public class MsSqlHandler
    {
        // SQL Server instance (LocalDB comes with Visual Studio)
        private readonly string server;

        // Name of the database on the server
        private readonly string dbName;

        // The open connection. It is null while we are not connected.
        private SqlConnection conn;

        // Constructor: lets us choose the server and the database name
        public MsSqlHandler(string server = @"(localdb)\MSSQLLocalDB", string dbName = "BrowserDb")
        {
            this.server = server;
            this.dbName = dbName;
        }

        // True when the connection exists and is open
        public bool IsConnected
        {
            get { return conn != null && conn.State == ConnectionState.Open; }
        }

        // Connects to the database.
        // If the database doesn't exist it is created, and the Keywords table is created
        // if it doesn't exist yet.
        public bool IsNewDatabase { get; private set; }
        public void ConnectToDb()
        {
            // Already connected: nothing to do
            if (IsConnected) return;

            // Creates the database the first time (we connect to "master" for this)
            using (var master = new SqlConnection(
                "Server=" + server + ";Database=master;Integrated Security=True"))
            {
                master.Open();

                using (var cmd = new SqlCommand("SELECT DB_ID(@name)", master))
                {
                    cmd.Parameters.AddWithValue("@name", dbName);
                    IsNewDatabase = cmd.ExecuteScalar() == DBNull.Value;
                }

                if (IsNewDatabase)
                {
                    // CREATE DATABASE can't take a parameter, so the name goes in brackets
                    using (var cmd = new SqlCommand("CREATE DATABASE [" + dbName + "]", master))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            conn = new SqlConnection(
                "Server=" + server + ";Database=" + dbName + ";Integrated Security=True");
            conn.Open();

            // Creates the table only if it doesn't exist (MSSQL has no IF NOT EXISTS here)
            string sql = @"IF OBJECT_ID('Keywords', 'U') IS NULL
                               CREATE TABLE Keywords (
                                   id INT IDENTITY(1,1) PRIMARY KEY,
                                   keyword NVARCHAR(200))";

            using (var cmd = new SqlCommand(sql, conn))
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

            using (var cmd = new SqlCommand(
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

            using (var cmd = new SqlCommand(
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

            using (var cmd = new SqlCommand("SELECT keyword FROM Keywords ORDER BY id", conn))
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
