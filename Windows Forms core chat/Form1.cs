/*
 * Form1.cs
 * ----------------------------------------------------------
 * Main Windows Forms controller for the Chat & TicTacToe Application.
 *
 * Purpose:
 * - Entry point and controller for all UI functionality.
 * - Hosts or joins a TCP-based chat session.
 * - Surfaces game UI for Tic-Tac-Toe and relays moves to the server.
 *
 * Features:
 * - Host a TCP chat server via TCPChatServer.
 * - Join an existing server via TCPChatClient.
 * - Live chat with moderation tools (!mod, !mods, !kick) when hosting.
 * - Authentication flow handled by server; client only relays commands.
 * - Keyboard Enter to send messages.
 * - Server-side Tic-Tac-Toe with local UI reflection and turn gating.
 *
 * Dependencies:
 * - TCPChatServer.cs, TCPChatClient.cs (networking and command logic)
 * - ClientSocket.cs (socket + metadata)
 * - TicTacToe.cs (board state + buttons)
 */


using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;


//https://www.youtube.com/watch?v=xgLRe7QV6QI&ab_channel=HazardEditHazardEdit
namespace Windows_Forms_Chat
{
    public partial class Form1 : Form
    {
        // TicTacToe logic and UI components
        public TicTacToe ticTacToe = new TicTacToe();

        // Server/client sockets
        public TCPChatServer server = null;
        public TCPChatClient client = null;

        #region Construction & Init
        public Form1()
        {
            InitializeComponent();
            // mono font helps align columns (e.g., !scores output)
            ChatTextBox.Font = new Font("Consolas", ChatTextBox.Font.Size);
        }

        // Form loaded: wire TicTacToe buttons + Enter-to-send
        private void Form1_Load(object sender, EventArgs e)
        {
            ticTacToe.buttons.Add(button1);
            ticTacToe.buttons.Add(button2);
            ticTacToe.buttons.Add(button3);
            ticTacToe.buttons.Add(button4);
            ticTacToe.buttons.Add(button5);
            ticTacToe.buttons.Add(button6);
            ticTacToe.buttons.Add(button7);
            ticTacToe.buttons.Add(button8);
            ticTacToe.buttons.Add(button9);

            SetGameBoardInteractable(false);

            // Enter key submits chat
            TypeTextBox.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter)
                {
                    SendButton_Click(SendButton, EventArgs.Empty);
                    ev.SuppressKeyPress = true;
                }
            };
        }

        // Checks if we can host or join (ensures we don’t have an active session)
        public bool CanHostOrJoin() => server == null && client == null;
        #endregion

        #region Host / Join
        // Handles Host button click
        private void HostButton_Click(object sender, EventArgs e)
        {
            if (!CanHostOrJoin()) return;

            try
            {
                int port = int.Parse(MyPortTextBox.Text);
                server = TCPChatServer.createInstance(port, ChatTextBox);
                if (server == null) throw new Exception("Invalid port or chat UI reference.");

                server.SetupServer();

                HostButton.Enabled = false;
                JoinButton.Enabled = false;
                SendButton.Enabled = true;
            }
            catch (SocketException ex)
            {
                server = null;
                HostButton.Enabled = true;
                JoinButton.Enabled = true;
                SendButton.Enabled = true;
                ChatTextBox.AppendText($"[Error]: Could not start server on port {MyPortTextBox.Text}.\n{ex.Message}" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                server = null;
                HostButton.Enabled = true;
                JoinButton.Enabled = true;
                SendButton.Enabled = true;
                ChatTextBox.AppendText($"[Error]: {ex.Message}" + Environment.NewLine);
            }
        }

        // Handles Join button click (connect to server)
        private void JoinButton_Click(object sender, EventArgs e)
        {
            if (!CanHostOrJoin()) return;

            try
            {
                int port = int.Parse(MyPortTextBox.Text);
                int serverPort = int.Parse(serverPortTextBox.Text);

                client = TCPChatClient.CreateInstance(port, serverPort, ServerIPTextBox.Text, ChatTextBox);
                if (client == null) throw new Exception("Incorrect port value!");

                client.OnConnectionFailed = () =>
                {
                    Invoke(new Action(() =>
                    {
                        ChatTextBox.AppendText("[Client]: Connection to server failed. Please try again." + Environment.NewLine);
                        JoinButton.Enabled = true;
                        HostButton.Enabled = true;
                        SendButton.Enabled = true;
                        client = null; // Reset for retry
                    }));
                };

                client.ConnectToServer(JoinButton, HostButton, SendButton);
            }
            catch (Exception ex)
            {
                client = null;
                ChatTextBox.AppendText("Error: " + ex.Message + Environment.NewLine);
            }
        }
        #endregion

        #region Chat Send + Host Command Dispatch
        // Send typed message OR dispatch host commands when hosting.
        private void SendButton_Click(object sender, EventArgs e)
        {
            string message = TypeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            if (server != null)
            {
                if (DispatchHostCommand(message))
                {
                    TypeTextBox.Clear();
                    return; // handled by host handler
                }

                // Normal server broadcast
                ChatTextBox.AppendText($"[Server]: {message}" + Environment.NewLine);
                server.SendToAll($"[Server]: {message}", null);
            }
            else if (client != null && client.socket.Connected)
            {
                client.SendString(message);
            }

            TypeTextBox.Clear();
        }

        // Routes host-only commands to handlers. Returns true if handled.
        private bool DispatchHostCommand(string msg)
        {
            if (msg.StartsWith("!mod ", StringComparison.OrdinalIgnoreCase))
                return HandleHostToggleModerator(msg.Substring(5).Trim());

            if (msg.Equals("!mods", StringComparison.OrdinalIgnoreCase))
                return HandleHostListModerators();

            if (msg.Equals("!dbtest", StringComparison.OrdinalIgnoreCase))
                return HandleHostDbTest();

            if (msg.StartsWith("!kick ", StringComparison.OrdinalIgnoreCase))
                return HandleHostKick(msg.Substring(6).Trim());

            return false;
        }

        // This handler method toggles the moderator flag for a client.
        private bool HandleHostToggleModerator(string modTarget)
        {
            string targetLower = modTarget.ToLowerInvariant();
            ClientSocket target = server.clientSockets.Find(c =>
                !string.IsNullOrEmpty(c.Username) &&
                c.Username.ToLowerInvariant() == targetLower);

            if (target != null)
            {
                target.IsModerator = !target.IsModerator;
                string status = target.IsModerator ? "promoted to moderator" : "demoted to regular user";

                ChatTextBox.AppendText($"[Server -> {target.Username}]: You have been {status}" + Environment.NewLine);
                string privateNote = $"[Server Notice]: You have been {status}";
                try
                {
                    target.socket.Send(Encoding.UTF8.GetBytes(privateNote + Environment.NewLine));
                }
                catch { /* ignore transient send errors */ }
            }
            else
            {
                ChatTextBox.AppendText($"[Server]: User '{modTarget}' not found." + Environment.NewLine);
            }
            return true;
        }

        // This handler method shows the current list of moderators.
        private bool HandleHostListModerators()
        {
            var sb = new StringBuilder("Current moderators:");
            bool hasMods = false;
            foreach (var c in server.clientSockets)
            {
                if (c.IsModerator)
                {
                    sb.Append(hasMods ? ", " : " ");
                    sb.Append(c.Username);
                    hasMods = true;
                }
            }
            if (!hasMods) sb.Append(" (none)");
            ChatTextBox.AppendText($"[Server]: {sb}" + Environment.NewLine);
            return true;
        }

        // This handler method is used for quick DB connectivity check.
        private bool HandleHostDbTest()
        {
            try
            {
                bool ok = DatabaseManager.TestConnection();
                ChatTextBox.AppendText(ok
                    ? "[Server]: Database connection successful." + Environment.NewLine
                    : "[Server]: Database test failed." + Environment.NewLine);
            }
            catch (Exception ex)
            {
                ChatTextBox.AppendText("[Server Error]: Database test failed - " + ex.Message + Environment.NewLine);
            }
            return true;
        }

        // This handler method is used by the server to remove a user.
        private bool HandleHostKick(string targetName)
        {
            string targetLower = targetName.ToLowerInvariant();

            var targetClient = server.clientSockets.Find(c =>
                !string.IsNullOrEmpty(c.Username) &&
                c.Username.ToLowerInvariant() == targetLower);

            if (targetClient != null)
            {
                try
                {
                    targetClient.socket.Send(Encoding.UTF8.GetBytes("You have been kicked by the Server.\n"));
                    server.SendToAll($"[{targetClient.Username}] was kicked by [Server]\n", targetClient);
                    ChatTextBox.AppendText($"[Server]: Kicked user {targetClient.Username}" + Environment.NewLine);

                    targetClient.socket.Shutdown(SocketShutdown.Both);
                    targetClient.socket.Close();
                    server.clientSockets.Remove(targetClient);
                }
                catch (Exception ex)
                {
                    ChatTextBox.AppendText($"[Server Error]: Failed to kick {targetName}. Reason: {ex.Message}" + Environment.NewLine);
                }
            }
            else
            {
                ChatTextBox.AppendText($"[Server]: User '{targetName}' not found." + Environment.NewLine);
            }
            return true;
        }
        #endregion

        #region Game Start + Board Wiring
        // Handles start game button.
        private void StartGameButton_Click(object sender, EventArgs e)
        {
            // Must be connected as client and be Player 1 (X)
            if (client == null || !client.socket.Connected || client.clientSocket.PlayerNumber != 1)
                return;

            // DB-backed presence check
            if (!GameStateManager.BothPlayersInGame())
            {
                client.AddToChat("[Server]: Both players must be present to start the game.\n");
                return;
            }

            client.SendString("!startgame");
            StartGameButton.Enabled = false;
            SetGameBoardInteractable(true); // server will send !yourturn/!waitturn
        }

        // Enables button if needed.
        public void TryEnableStartButton()
        {
            // Only Player 1 can start
            if (client != null &&
                client.clientSocket.State == ClientState.Playing &&
                client.clientSocket.PlayerNumber == 1 &&
                !StartGameButton.Enabled)
            {
                StartGameButton.Enabled = true;
            }
        }

        // Enable/disable all board buttons + color
        public void SetGameBoardInteractable(bool isEnabled)
        {
            foreach (var btn in ticTacToe.buttons)
            {
                btn.Enabled = isEnabled;
                btn.BackColor = isEnabled ? Color.Violet : Color.Gray;
            }
        }

        // Attempt a move (0-8); server validates legality/turn
        private void AttemptMove(int i)
        {
            if (ticTacToe.myTurn && client != null && client.socket.Connected)
            {
                client.SendString($"!move {i}");
            }
        }

        // Below are handlers for each TicTacToe button (0-8)
        private void button1_Click(object sender, EventArgs e) => AttemptMove(0);
        private void button2_Click(object sender, EventArgs e) => AttemptMove(1);
        private void button3_Click(object sender, EventArgs e) => AttemptMove(2);
        private void button4_Click(object sender, EventArgs e) => AttemptMove(3);
        private void button5_Click(object sender, EventArgs e) => AttemptMove(4);
        private void button6_Click(object sender, EventArgs e) => AttemptMove(5);
        private void button7_Click(object sender, EventArgs e) => AttemptMove(6);
        private void button8_Click(object sender, EventArgs e) => AttemptMove(7);
        private void button9_Click(object sender, EventArgs e) => AttemptMove(8);
    }
    #endregion
}
