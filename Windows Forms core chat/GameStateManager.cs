/*
 * GameStateManager.cs
 * ----------------------------------------------------------
 * Centralized persistent Tic-Tac-Toe game state for the server.
 *
 * Purpose:
 * - Persist player assignments and the current turn in SQLite.
 * - Maintain a headless server-side TicTacToe board (no UI).
 * - Provide a small API used by TCPChatServer to run games.
 *
 * Features:
 * - Local SQLite file: gamestate.db
 * - Key/Value table: GameState (keys: Player1, Player2, CurrentTurn)
 * - Thread-safe board mirror for move validation/results
 * - Helper queries (BothPlayersInGame, CanStartGame, etc.)
 *
 * Dependencies:
 * - System.Data.SQLite.Core
 * - TicTacToe.cs (board rules/evaluation)
 */

using System;
using System.Data.SQLite;
using System.IO;

namespace Windows_Forms_Chat
{
    public static class GameStateManager
    {
        private static readonly string dbFile = "gamestate.db";
        private static readonly string connectionString = $"Data Source={dbFile};Version=3;";

        // Headless server board (no UI). Guarded by _boardLock for safety.
        private static readonly object _boardLock = new object();
        private static readonly TicTacToe _board = new TicTacToe
        {
            myTurn = false,
            playerTileType = TileType.blank
        };

        #region Initialization & Schema
        static GameStateManager()
        {
            Initialize();
        }

        /// <summary>
        /// Creates the database (if needed) and ensures the GameState table exists.
        /// </summary>
        public static void Initialize()
        {
            if (!File.Exists(dbFile))
                SQLiteConnection.CreateFile(dbFile);

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                const string createTable = @"
                    CREATE TABLE IF NOT EXISTS GameState (
                        Key   TEXT PRIMARY KEY,
                        Value TEXT
                    );";

                using (var cmd = new SQLiteCommand(createTable, conn))
                    cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region Private Helpers (Key/Value)

        /// <summary>
        /// Inserts or updates a key/value pair in the GameState table.
        /// </summary>
        private static void SetValue(string key, string value)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                const string sql = @"
                    INSERT INTO GameState (Key, Value) VALUES (@k, @v)
                    ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@k", key);
                    cmd.Parameters.AddWithValue("@v", value ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Retrieves the value for a given key from the GameState table.
        /// Returns null if the key does not exist.
        /// </summary>
        private static string GetValue(string key)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                const string sql = "SELECT Value FROM GameState WHERE Key = @k";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@k", key);
                    object result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }

        #endregion

        #region Public API - Players & Turn

        /// <summary>Sets Player 1 username (display-casing preserved).</summary>
        public static void SetPlayer1(string username) => SetValue("Player1", username);

        /// <summary>Sets Player 2 username (display-casing preserved).</summary>
        public static void SetPlayer2(string username) => SetValue("Player2", username);

        /// <summary>Sets the username of the player whose turn it is.</summary>
        public static void SetCurrentTurn(string username) => SetValue("CurrentTurn", username);

        /// <summary>Returns Player 1 username (or null).</summary>
        public static string GetPlayer1() => GetValue("Player1");

        /// <summary>Returns Player 2 username (or null).</summary>
        public static string GetPlayer2() => GetValue("Player2");

        /// <summary>Returns the username whose turn it is (or null).</summary>
        public static string GetCurrentTurn() => GetValue("CurrentTurn");

        /// <summary>Clears Player 1 assignment.</summary>
        public static void ClearPlayer1() => SetPlayer1(null);

        /// <summary>Clears Player 2 assignment.</summary>
        public static void ClearPlayer2() => SetPlayer2(null);

        /// <summary>Clears the current turn indicator.</summary>
        public static void ClearCurrentTurn() => SetCurrentTurn(null);

        #endregion

        #region Public API - Server Board

        /// <summary>
        /// Attempts to place a tile on the server board. Returns true on success.
        /// </summary>
        public static bool SetTile(int index, TileType tile)
        {
            lock (_boardLock)
                return _board.SetTile(index, tile);
        }

        /// <summary>
        /// Evaluates and returns the current game state (playing/win/draw).
        /// </summary>
        public static GameState GetGameState()
        {
            lock (_boardLock)
                return _board.GetGameState();
        }

        /// <summary>
        /// Resets only the server board to blanks.
        /// </summary>
        public static void ResetBoard()
        {
            lock (_boardLock)
                _board.ResetBoard();
        }

        /// <summary>
        /// Resets players, turn, and the board—ready for a new game.
        /// </summary>
        public static void ResetGame()
        {
            ClearPlayer1();
            ClearPlayer2();
            ClearCurrentTurn();
            ResetBoard();
        }
        #endregion

        #region State Queries

        /// <summary>True if a turn is assigned (game started).</summary>
        public static bool IsGameInProgress() => !string.IsNullOrEmpty(GetCurrentTurn());

        /// <summary>True if Player1 and Player2 are both set.</summary>
        public static bool BothPlayersInGame() =>
            !string.IsNullOrEmpty(GetPlayer1()) && !string.IsNullOrEmpty(GetPlayer2());

        /// <summary>True if username matches either player slot.</summary>
        public static bool IsPlayer(string username) =>
            username == GetPlayer1() || username == GetPlayer2();

        /// <summary>True if username is Player1.</summary>
        public static bool IsPlayer1(string username) => username == GetPlayer1();

        /// <summary>True if username is Player2.</summary>
        public static bool IsPlayer2(string username) => username == GetPlayer2();

        /// <summary>True if both players are set and no one has started a turn yet.</summary>
        public static bool CanStartGame() => BothPlayersInGame() && !IsGameInProgress();

        #endregion
    }
}
