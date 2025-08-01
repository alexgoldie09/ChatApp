/*
 * Form1.cs
 * ----------------------------------------------------------
 * Main Windows Forms controller for the Chat & TicTacToe Application.
 *
 * Purpose:
 * - Acts as the entry point and controller for all UI-related functionality.
 * - Allows users to host or join a TCP-based chat session.
 * - Provides a basic turn-based Tic Tac Toe game embedded in the same window.
 *
 * Features:
 * - Hosting a TCP chat server on a user-defined port using TCPChatServer.
 * - Joining an existing server via TCPChatClient with live username negotiation.
 * - Live chat system with user-to-user and server-wide communication.
 * - Moderator controls (!mod, !mods, !kick) available to the host.
 * - Username management including initial setting (!username) and changes (!user).
 * - UI feedback on failed connections or rejected usernames.
 * - Graceful disconnection and rejoining support.
 * - Message sanitization and empty message checks.
 * - Chat input box supports Enter key submission.
 * - Embedded Tic Tac Toe game logic with win/draw/reset checks.
 * - Visual feedback and chat notifications for Tic Tac Toe game outcomes.
 *
 * Dependencies:
 * - TCPChatServer.cs: server-side socket handling and broadcast logic.
 * - TCPChatClient.cs: client-side connection and send logic.
 * - ClientSocket.cs: wrapper class for connected clients (username, socket, moderation).
 * - TicTacToe.cs: game logic and board state management for Tic Tac Toe.
 */


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        public Form1()
        {
            InitializeComponent();
        }

        // Checks if we can host or join (ensures we don’t have an active session)
        public bool CanHostOrJoin()
        {
            return server == null && client == null;
        }

        // Handles Host button click
        private void HostButton_Click(object sender, EventArgs e)
        {
            if (CanHostOrJoin())
            {
                try
                {
                    int port = int.Parse(MyPortTextBox.Text);
                    server = TCPChatServer.createInstance(port, ChatTextBox);

                    if (server == null)
                        throw new Exception("Invalid port or chat UI reference.");

                    server.SetupServer();

                    // Disable Host/Join buttons; enable Send
                    HostButton.Enabled = false;
                    JoinButton.Enabled = false;
                    SendButton.Enabled = true;
                }
                catch (SocketException ex)
                {
                    // Port in use or error; show message and allow retry
                    server = null;
                    HostButton.Enabled = true;
                    JoinButton.Enabled = true;
                    SendButton.Enabled = true;
                    ChatTextBox.AppendText($"[Error]: Could not start server on port {MyPortTextBox.Text}.\n{ex.Message}" + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Other exception
                    server = null;
                    HostButton.Enabled = true;
                    JoinButton.Enabled = true;
                    SendButton.Enabled = true;
                    ChatTextBox.AppendText($"[Error]: {ex.Message}" + Environment.NewLine);
                }
            }
        }

        // Handles Join button click (connect to server)
        private void JoinButton_Click(object sender, EventArgs e)
        {
            if (CanHostOrJoin())
            {
                try
                {
                    int port = int.Parse(MyPortTextBox.Text);
                    int serverPort = int.Parse(serverPortTextBox.Text);

                    // Create new client instance
                    client = TCPChatClient.CreateInstance(port, serverPort, ServerIPTextBox.Text, ChatTextBox);

                    if (client == null)
                        throw new Exception("Incorrect port value!");

                    // Register callback if unable to connect to the server
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

                    // Start connection attempt
                    client.ConnectToServer(JoinButton, HostButton, SendButton);

                }
                catch (Exception ex)
                {
                    client = null;
                    ChatTextBox.AppendText("Error: " + ex.Message + Environment.NewLine);
                }
            }
        }


        // Handles Send button click (send typed message)
        private void SendButton_Click(object sender, EventArgs e)
        {
            string message = TypeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            if (server != null)
            {
                // --- Host-only commands ---
                if (message.StartsWith("!mod ", StringComparison.OrdinalIgnoreCase))
                {
                    string modTarget = message.Substring(5).Trim();
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
                        target.socket.Send(Encoding.UTF8.GetBytes(privateNote));
                    }
                    else
                    {
                        ChatTextBox.AppendText($"[Server]: User '{modTarget}' not found." + Environment.NewLine);
                    }
                    TypeTextBox.Clear();
                    return;
                }
                else if (message == "!mods")
                {
                    StringBuilder modList = new StringBuilder("Current moderators:");
                    bool hasMods = false;
                    foreach (var c in server.clientSockets)
                    {
                        if (c.IsModerator)
                        {
                            modList.Append(hasMods ? ", " : " ");
                            modList.Append($"{c.Username}");
                            hasMods = true;
                        }
                    }
                    if (!hasMods) modList.Append(" (none)");
                    ChatTextBox.AppendText($"[Server]: {modList}" + Environment.NewLine);
                    TypeTextBox.Clear();
                    return;
                }
                else if (message == "!dbtest")
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
                    TypeTextBox.Clear();
                    return;
                }
                else if (message.StartsWith("!kick ", StringComparison.OrdinalIgnoreCase))
                {
                    string targetName = message.Substring(6).Trim();
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
                    TypeTextBox.Clear();
                    return;
                }
                else
                {
                    // Normal message broadcast
                    ChatTextBox.AppendText($"[Server]: {message}" + Environment.NewLine);
                    server.SendToAll($"[Server]: {message}", null);
                }
            }
            else if (client != null && client.socket.Connected)
            {
                // No more username checks here —
                // Server enforces Login/Register before allowing chat
                client.SendString(message);
            }

            TypeTextBox.Clear(); // Clear message input box
        }

        // Form loaded: setup TicTacToe UI buttons
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

            // Listen for Enter key in the chat input field
            TypeTextBox.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter)
                {
                    SendButton_Click(SendButton, EventArgs.Empty);  // Simulate send
                    ev.SuppressKeyPress = true;  // Prevent ding sound or new line
                }
            };
        }

        // Shared method for trying a TicTacToe move
        private void AttemptMove(int i)
        {
            if (ticTacToe.myTurn)
            {
                bool validMove = ticTacToe.SetTile(i, ticTacToe.playerTileType);
                if (validMove)
                {
                    //tell server about it
                    //ticTacToe.myTurn = false;//call this too when ready with server
                }
                //example, do something similar from server

                // Check win/draw conditions
                GameState gs = ticTacToe.GetGameState();
                if (gs == GameState.crossWins)
                {
                    ChatTextBox.AppendText("X wins!");
                    ChatTextBox.AppendText(Environment.NewLine);
                    ticTacToe.ResetBoard();
                }
                if (gs == GameState.naughtWins)
                {
                    ChatTextBox.AppendText(") wins!");
                    ChatTextBox.AppendText(Environment.NewLine);
                    ticTacToe.ResetBoard();
                }
                if (gs == GameState.draw)
                {
                    ChatTextBox.AppendText("Draw!");
                    ChatTextBox.AppendText(Environment.NewLine);
                    ticTacToe.ResetBoard();
                }
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
}
