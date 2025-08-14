using System;
using System.Data.SQLite;
using System.IO;

namespace Windows_Forms_Chat
{
    public static class GameStateManager
    {
        private static readonly string dbFile = "gamestate.db";
        private static readonly string connectionString = $"Data Source={dbFile};Version=3;";

        static GameStateManager()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (!File.Exists(dbFile))
                SQLiteConnection.CreateFile(dbFile);

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string createTable = @"
                CREATE TABLE IF NOT EXISTS GameState (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );";

                using (var cmd = new SQLiteCommand(createTable, conn))
                    cmd.ExecuteNonQuery();
            }
        }

        private static void SetValue(string key, string value)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string sql = @"
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

        private static string GetValue(string key)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string sql = "SELECT Value FROM GameState WHERE Key = @k";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@k", key);
                    object result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }

        // ---- API ----

        public static void SetPlayer1(string username) => SetValue("Player1", username);
        public static void SetPlayer2(string username) => SetValue("Player2", username);
        public static void SetCurrentTurn(string username) => SetValue("CurrentTurn", username);

        public static string GetPlayer1() => GetValue("Player1");
        public static string GetPlayer2() => GetValue("Player2");
        public static string GetCurrentTurn() => GetValue("CurrentTurn");

        public static void ClearPlayer1() => SetPlayer1(null);
        public static void ClearPlayer2() => SetPlayer2(null);
        public static void ClearCurrentTurn() => SetCurrentTurn(null);

        // ----- Board delegation to a single headless TicTacToe -----
        private static readonly object _boardLock = new object();
        private static readonly TicTacToe _board = new TicTacToe
        {
            // headless: no buttons; server doesn’t render UI
            myTurn = false,
            playerTileType = TileType.blank
        };

        public static bool SetTile(int index, TileType tile)
        {
            lock (_boardLock)
                return _board.SetTile(index, tile);
        }

        public static GameState GetGameState()
        {
            lock (_boardLock)
                return _board.GetGameState();
        }

        public static void ResetBoard()
        {
            lock (_boardLock)
                _board.ResetBoard();
        }

        public static void ResetGame()
        {
            ClearPlayer1();
            ClearPlayer2();
            ClearCurrentTurn();
            ResetBoard();
        }

        public static bool IsGameInProgress() => !string.IsNullOrEmpty(GetCurrentTurn());
        public static bool BothPlayersInGame() =>
            !string.IsNullOrEmpty(GetPlayer1()) && !string.IsNullOrEmpty(GetPlayer2());
        public static bool IsPlayer(string username) =>
            username == GetPlayer1() || username == GetPlayer2();
        public static bool IsPlayer1(string username) => username == GetPlayer1();
        public static bool IsPlayer2(string username) => username == GetPlayer2();
        public static bool CanStartGame() => BothPlayersInGame() && !IsGameInProgress();
    }
}
