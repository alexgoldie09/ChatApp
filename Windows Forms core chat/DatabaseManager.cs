/*
 * DatabaseManager.cs
 * ----------------------------------------------------------
 * Centralized SQLite access for authentication and game stats.
 *
 * Purpose:
 * - Create/open the app database and ensure the Users table exists.
 * - Provide helpers for register/login/rename flows.
 * - Track Tic-Tac-Toe results (Wins/Losses/Draws) and report leaderboards.
 *
 * Features:
 * - Local SQLite file: chatapp.db
 * - Users schema:
 *      ID (PK), Username (UNIQUE, NOCASE), Password, Wins, Losses, Draws
 * - Case-insensitive uniqueness while preserving display casing.
 * - Simple stats API (increment/get) and a leaderboard getter.
 *
 * Dependencies:
 * - System.Data.SQLite.Core
 */

using System;
using System.Collections.Generic;
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

        #region Initialization & Schema
        /// <summary>
        /// Creates the database file (if needed) and ensures the Users table exists.
        /// Throws on failure so the server can surface the error.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (!File.Exists(dbFile))
                    SQLiteConnection.CreateFile(dbFile);

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    const string createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
                            Password TEXT NOT NULL,
                            Wins INTEGER DEFAULT 0,
                            Losses INTEGER DEFAULT 0,
                            Draws INTEGER DEFAULT 0
                        );";

                    using (var command = new SQLiteCommand(createTableQuery, connection))
                        command.ExecuteNonQuery();
                }
            }
            catch
            {
                // Bubble up to server
                throw;
            }
        }

        /// <summary>
        /// Lightweight connectivity probe. Returns true if a connection opens successfully.
        /// </summary>
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
        #endregion

        #region Validation
        /// <summary>
        /// Validates username format and reserved words.
        /// </summary>
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
        #endregion

        #region User Lifecycle (Register / Login / Rename)
        /// <summary>
        /// Attempts to register a new user. Returns false with errorMessage if name is invalid or taken.
        /// </summary>
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

                    const string insertSql = "INSERT INTO Users (Username, Password) VALUES (@u, @p)";
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

        /// <summary>
        /// Attempts to log in. On success, returns the stored displayName (original casing).
        /// </summary>
        public static bool TryLogin(string username, string password, out string errorMessage, out string displayName)
        {
            errorMessage = null;
            displayName = null;

            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    const string sql = "SELECT Username, Password FROM Users WHERE Username = @u COLLATE NOCASE";
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

        /// <summary>
        /// Attempts to rename an existing user to a new, valid username.
        /// </summary>
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

                    const string updateSql = "UPDATE Users SET Username = @new WHERE Username = @old COLLATE NOCASE";
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
        #endregion

        #region Stats API (Increment / Read)
        /// <summary>Wins++ for a user (NOCASE match).</summary>
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

        /// <summary>Losses++ for a user (NOCASE match).</summary>
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

        /// <summary>Draws++ for a user (NOCASE match).</summary>
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

        /// <summary>
        /// Returns (Wins, Losses, Draws) for a given user. Returns (0,0,0) if not found.
        /// </summary>
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
        #endregion

        #region Leaderboard API
        /// <summary>
        /// Returns all user scores sorted by Wins desc, then Draws desc.
        /// </summary>
        public static List<(string Username, int Wins, int Losses, int Draws)> GetAllScores()
        {
            var scores = new List<(string Username, int Wins, int Losses, int Draws)>();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                const string sql = "SELECT Username, Wins, Losses, Draws FROM Users ORDER BY Wins DESC, Draws DESC";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        scores.Add((
                            reader.GetString(0),
                            reader.GetInt32(1),
                            reader.GetInt32(2),
                            reader.GetInt32(3)
                        ));
                    }
                }
            }

            return scores;
        }
        #endregion
    }
}
