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
        //TODO change myTurn to false and playerTileType to blank for defaults
        //they should be dictated by the server
        public bool myTurn = true;
        public TileType playerTileType = TileType.cross;
        public List<Button> buttons = new List<Button>();//assuming 9 in order
        public TileType[] grid = new TileType[9];

        public string GridToString()
        {
            var sb = new StringBuilder(9);
            for (int i = 0; i < 9; i++)
            {
                sb.Append(grid[i] == TileType.blank ? '_'
                          : grid[i] == TileType.cross ? 'x' : 'o');
            }
            return sb.ToString();
        }

        public void StringToGrid(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 9) return;
            for (int i = 0; i < 9; i++)
            {
                char c = char.ToLowerInvariant(s[i]);
                grid[i] = c == 'x' ? TileType.cross
                        : c == 'o' ? TileType.naught
                        : TileType.blank;

                if (buttons.Count >= 9)
                    buttons[i].Text = TileTypeToString(grid[i]);
            }
        }

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

            // was returning true even when occupied — fix to false
            return false;
        }

        public GameState GetGameState()
        {
            GameState state = GameState.playing;
            if (CheckForWin(TileType.cross))
                state = GameState.crossWins;
            else if (CheckForWin(TileType.naught))
                state = GameState.naughtWins;
            else if (CheckForDraw())
                state = GameState.draw;


            return state;
        }
        public bool CheckForWin(TileType t)
        {
            //horizontals
            if (grid[0] == t && grid[1] == t && grid[2] == t)
                return true;
            if (grid[3] == t && grid[4] == t && grid[5] == t)
                return true;
            if (grid[6] == t && grid[7] == t && grid[8] == t)
                return true;

            //verticals
            if (grid[0] == t && grid[3] == t && grid[6] == t)
                return true;
            if (grid[1] == t && grid[4] == t && grid[7] == t)
                return true;
            if (grid[2] == t && grid[5] == t && grid[8] == t)
                return true;

            //diagonals
            if (grid[0] == t && grid[4] == t && grid[8] == t)
                return true;
            if (grid[2] == t && grid[4] == t && grid[6] == t)
                return true;


            //nothing
            return false;
        }

        public bool CheckForDraw()
        {
            for(int i = 0; i < 9; i++)
            {
                if (grid[i] == TileType.blank)
                    return false;
            }

            return true;
        }

        public void ResetBoard()
        {
            for (int i = 0; i < 9; i++)
            {
                grid[i] = TileType.blank;
                if (buttons.Count >= 9)
                    buttons[i].Text = TileTypeToString(TileType.blank);
            }
        }

        public static string TileTypeToString(TileType t)
        {
            if (t == TileType.blank)
                return "";
            else if (t == TileType.cross)
                return "X";
            else
                return "O";
        }
    }
}
