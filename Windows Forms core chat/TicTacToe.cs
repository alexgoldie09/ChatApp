/*
 * TicTacToe.cs
 * ----------------------------------------------------------
 * Lightweight board + UI helper for Tic-Tac-Toe.
 *
 * Purpose:
 * - Represent and mutate a 3x3 grid using an enum-based model.
 * - Provide helpers to reflect board state onto WinForms buttons.
 * - Offer win/draw detection and simple (de)serialization.
 *
 * Features:
 * - Enum TileType: blank / cross (X) / naught (O)
 * - Enum GameState: playing / draw / crossWins / naughtWins
 * - String encoding: "xox___x_o" (x, o, _), length=9
 * - Helpers to set tiles, reset, and sync UI text.
 *
 * Notes:
 * - Interaction (turn gating) is enforced by the server/client layers.
 * - This class only updates button text (not colors or enabled state).
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{
    public enum TileType
    {
        blank, cross, naught
    }
    public enum GameState
    {
        playing, draw, crossWins, naughtWins
    }

    public class TicTacToe
    {
        // These defaults are dictated by the server at runtime.
        public bool myTurn = false;                    // server toggles via !yourturn/!waitturn
        public TileType playerTileType = TileType.blank; // server sets to cross/naught on join

        // Buttons must be assigned in board order (0..8) by the Form.
        public List<Button> buttons = new List<Button>();
        public TileType[] grid = new TileType[9];

        #region Serialization
        /// <summary>
        /// Converts the 3x3 grid to a 9-char string: 'x','o','_'.
        /// Example: "xox___x_o"
        /// </summary>
        public string GridToString()
        {
            var sb = new StringBuilder(9);
            for (int i = 0; i < 9; i++)
            {
                sb.Append(
                    grid[i] == TileType.blank ? '_' :
                    grid[i] == TileType.cross ? 'x' :
                                                 'o'
                );
            }
            return sb.ToString();
        }

        /// <summary>
        /// Loads a 9-char string (x/o/_) into the grid and reflects to buttons.
        /// Ignores invalid inputs (null or length != 9).
        /// </summary>
        public void StringToGrid(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 9) return;

            for (int i = 0; i < 9; i++)
            {
                char c = char.ToLowerInvariant(s[i]);
                grid[i] =
                    c == 'x' ? TileType.cross :
                    c == 'o' ? TileType.naught :
                               TileType.blank;

                if (buttons.Count >= 9)
                    buttons[i].Text = TileTypeToString(grid[i]);
            }
        }
        #endregion

        #region Mutations
        /// <summary>
        /// Attempts to place a tile (X/O) at index [0..8].
        /// Returns true if placed; false if out of range, blank requested, or occupied.
        /// </summary>
        public bool SetTile(int index, TileType tileType)
        {
            if (index < 0 || index > 8) return false;
            if (tileType == TileType.blank) return false;

            if (grid[index] == TileType.blank)
            {
                grid[index] = tileType;
                if (buttons.Count >= 9)
                    buttons[index].Text = TileTypeToString(tileType);
                return true;
            }

            // Occupied tile
            return false;
        }

        /// <summary>
        /// Clears the board to blank and updates button text.
        /// </summary>
        public void ResetBoard()
        {
            for (int i = 0; i < 9; i++)
            {
                grid[i] = TileType.blank;
                if (buttons.Count >= 9)
                    buttons[i].Text = TileTypeToString(TileType.blank);
            }
        }
        #endregion

        #region Evaluation

        /// <summary>
        /// Determines current game state: playing, crossWins, naughtWins, or draw.
        /// </summary>
        public GameState GetGameState()
        {
            if (CheckForWin(TileType.cross))
                return GameState.crossWins;

            if (CheckForWin(TileType.naught))
                return GameState.naughtWins;

            return CheckForDraw() ? GameState.draw : GameState.playing;
        }

        /// <summary>
        /// True if the given tile type holds any winning 3-in-a-row.
        /// </summary>
        public bool CheckForWin(TileType t)
        {
            // horizontals
            if (grid[0] == t && grid[1] == t && grid[2] == t) return true;
            if (grid[3] == t && grid[4] == t && grid[5] == t) return true;
            if (grid[6] == t && grid[7] == t && grid[8] == t) return true;

            // verticals
            if (grid[0] == t && grid[3] == t && grid[6] == t) return true;
            if (grid[1] == t && grid[4] == t && grid[7] == t) return true;
            if (grid[2] == t && grid[5] == t && grid[8] == t) return true;

            // diagonals
            if (grid[0] == t && grid[4] == t && grid[8] == t) return true;
            if (grid[2] == t && grid[4] == t && grid[6] == t) return true;

            return false;
        }

        /// <summary>
        /// True if no blanks remain (and no winner).
        /// </summary>
        public bool CheckForDraw()
        {
            for (int i = 0; i < 9; i++)
                if (grid[i] == TileType.blank)
                    return false;

            return true;
        }

        #endregion

        #region Display Helpers

        /// <summary>
        /// Converts a tile to its display character: "" / "X" / "O".
        /// </summary>
        public static string TileTypeToString(TileType t)
        {
            if (t == TileType.blank) return "";
            if (t == TileType.cross) return "X";
            return "O";
        }

        #endregion
    }
}
