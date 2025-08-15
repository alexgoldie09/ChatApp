/*
 * DatabaseManager.cs
 * ----------------------------------------------------------
 * Manages SQLite database connection and user table setup.
 *
 * Purpose:
 * - Provides centralized access to SQLite database logic.
 * - Ensures the Users table exists before the server starts accepting clients.
 * - Adds authentication helpers for login/register.
 *
 * Features:
 * - Creates/open a local SQLite database file (`chatapp.db`).
 * - Creates Users table if it does not exist, with schema:
 *      ID (PK), Username (case-insensitive UNIQUE), Password, Wins, Losses, Draws
 * - Enforces case-insensitive uniqueness while preserving chosen display casing.
 *
 * Dependencies:
 * - Requires System.Data.SQLite.Core NuGet package.
 */

using System;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;

namespace Windows_Forms_Chat
{
    public static class DatabaseManager
    {
        private static readonly string dbFile = "chatapp.db";
        private static readonly string connectionString = $"Data Source={dbFile};Version=3;";
        private static readonly string[] ReservedNames = { "host", "server", "admin", "moderator" };

        public static void Initialize()
        {
            try
            {
                if (!File.Exists(dbFile))
                {
                    SQLiteConnection.CreateFile(dbFile);
                }

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
                            Password TEXT NOT NULL,
                            Wins INTEGER DEFAULT 0,
                            Losses INTEGER DEFAULT 0,
                            Draws INTEGER DEFAULT 0
                        );";

                    using (var command = new SQLiteCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                throw; // bubble up to server
            }
        }

        public static bool TestConnection()
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // Validation rules
        public static bool ValidateUsername(string username, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "Username cannot be empty.";
                return false;
            }

            username = username.Trim();

            if (username.Length < 3 || username.Length > 16)
            {
                errorMessage = "Username must be between 3 and 16 characters.";
                return false;
            }

            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
            {
                errorMessage = "Username can only contain letters, numbers, and underscores.";
                return false;
            }

            foreach (string reserved in ReservedNames)
            {
                if (username.Equals(reserved, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = $"The username '{username}' is reserved.";
                    return false;
                }
            }

            return true;
        }

        // Register
        public static bool TryRegister(string username, string password, out string errorMessage)
        {
            errorMessage = null;
            string display = username.Trim();

            if (!ValidateUsername(display, out errorMessage))
                return false;

            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    string insertSql = "INSERT INTO Users (Username, Password) VALUES (@u, @p)";
                    using (var cmd = new SQLiteCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", display);
                        cmd.Parameters.AddWithValue("@p", password);
                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                errorMessage = "Username already exists.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Database error: {ex.Message}";
                return false;
            }
        }

        // Login
        public static bool TryLogin(string username, string password, out string errorMessage, out string displayName)
        {
            errorMessage = null;
            displayName = null;

            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    string sql = "SELECT Username, Password FROM Users WHERE Username = @u COLLATE NOCASE";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username.Trim());

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedDisplay = reader.GetString(0);
                                string storedPassword = reader.GetString(1);

                                if (storedPassword == password)
                                {
                                    displayName = storedDisplay;
                                    return true;
                                }
                                else
                                {
                                    errorMessage = "Invalid password.";
                                    return false;
                                }
                            }
                            else
                            {
                                errorMessage = "User not found.";
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Database error: {ex.Message}";
                return false;
            }
        }

        // Rename
        public static bool TryUpdateUsername(string oldUsername, string newUsername, out string errorMessage)
        {
            errorMessage = null;

            if (!ValidateUsername(newUsername, out errorMessage))
                return false;

            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    string updateSql = "UPDATE Users SET Username = @new WHERE Username = @old COLLATE NOCASE";
                    using (var cmd = new SQLiteCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@new", newUsername.Trim());
                        cmd.Parameters.AddWithValue("@old", oldUsername.Trim());

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0) return true;

                        errorMessage = $"Old username '{oldUsername}' not found.";
                        return false;
                    }
                }
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                errorMessage = $"Username '{newUsername}' is already taken.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Database error: {ex.Message}";
                return false;
            }
        }

        public static void IncrementWins(string username)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE Users SET Wins = Wins + 1 WHERE Username = @u COLLATE NOCASE", conn))
                {
                    cmd.Parameters.AddWithValue("@u", username.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void IncrementLosses(string username)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE Users SET Losses = Losses + 1 WHERE Username = @u COLLATE NOCASE", conn))
                {
                    cmd.Parameters.AddWithValue("@u", username.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void IncrementDraws(string username)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE Users SET Draws = Draws + 1 WHERE Username = @u COLLATE NOCASE", conn))
                {
                    cmd.Parameters.AddWithValue("@u", username.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static (int wins, int losses, int draws) GetStats(string username)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Wins, Losses, Draws FROM Users WHERE Username = @u COLLATE NOCASE", conn))
                {
                    cmd.Parameters.AddWithValue("@u", username.Trim());
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            int w = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                            int l = r.IsDBNull(1) ? 0 : r.GetInt32(1);
                            int d = r.IsDBNull(2) ? 0 : r.GetInt32(2);
                            return (w, l, d);
                        }
                    }
                }
            }
            return (0, 0, 0);
        }

    }
}
