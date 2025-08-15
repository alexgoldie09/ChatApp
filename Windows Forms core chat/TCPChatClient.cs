/*
 * TCPChatClient.cs
 * ----------------------------------------------------------
 * Handles TCP client functionality for joining a chat server.
 *
 * Purpose:
 * - Establishes a TCP connection to a specified remote chat server.
 * - Manages sending and receiving messages from the server.
 * - Processes command-based messages (e.g., !login, !register, !user, !join, !move).
 *
 * Features:
 * - Connection retry with 10 attempts.
 * - SQLite-backed auth flow via server commands.
 * - UI buttons auto-enable/disable based on connection/game state.
 * - Self-message replacement ([Me]) for local echo.
 * - Graceful disconnect + lobby reset on session end.
 *
 * Dependencies:
 * - TCPChatBase.cs (shared UI helpers and newline handling)
 * - ClientSocket.cs (buffer/socket, state, player number)
 * - System.Net.Sockets (async socket)
 * - System.Windows.Forms (UI)
 */

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{
    public class TCPChatClient : TCPChatBase
    {
        // Main socket
        public Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // Per-client metadata/buffer holder
        public ClientSocket clientSocket = new ClientSocket();

        // Remote endpoint
        public int serverPort;
        public string serverIP;

        // Local identity flags
        public bool usernameAccepted = false;
        public string username;

        // Disconnect guard
        private bool hasDisconnected = false;

        // External callbacks
        public Action OnDisconnected;
        public Action OnConnectionFailed;

        #region Setup & Lifecycle
        // Factory method to validate and create a TCPChatClient instance.
        public static TCPChatClient CreateInstance(int port, int serverPort, string serverIP, TextBox chatTextBox)
        {
            TCPChatClient tcp = null;

            if (port > 0 && port < 65535 &&
                serverPort > 0 && serverPort < 65535 &&
                !string.IsNullOrWhiteSpace(serverIP) &&
                chatTextBox != null)
            {
                tcp = new TCPChatClient
                {
                    port = port,
                    serverPort = serverPort,
                    serverIP = serverIP,
                    chatTextBox = chatTextBox
                };
                tcp.clientSocket.socket = tcp.socket;
            }

            // Default disconnect cleanup
            tcp.OnDisconnected = () =>
            {
                chatTextBox.Invoke(new Action(() =>
                {
                    chatTextBox.AppendText("[Client]: Session Closed." + Environment.NewLine);

                    if (chatTextBox.FindForm() is Form1 form)
                    {
                        form.JoinButton.Enabled = true;
                        form.HostButton.Enabled = true;
                        form.SendButton.Enabled = false;
                        form.StartGameButton.Enabled = false;

                        form.ticTacToe.ResetBoard();
                        form.ticTacToe.myTurn = false;
                        form.SetGameBoardInteractable(false);

                        form.client = null;
                    }
                }));
            };

            return tcp;
        }

        // Attempts to connect to the TCP server. Disables UI buttons during retry.
        public void ConnectToServer(Button joinButton, Button hostButton, Button sendButton)
        {
            // Disable UI during connection attempt
            joinButton.Enabled = false;
            hostButton.Enabled = false;
            sendButton.Enabled = false;

            int attempts = 0;
            bool connected = false;

            // Try up to 10 times to connect
            while (!connected && attempts < 10)
            {
                try
                {
                    attempts++;
                    SetChat("[Client]: Connection attempt " + attempts);
                    socket.Connect(serverIP, serverPort);
                    clientSocket.socket = socket;
                    connected = true;
                }
                catch (SocketException)
                {
                    Thread.Sleep(300);
                }
            }

            if (!connected)
            {
                AddToChat("[Client]: Unable to connect to server after 10 attempts. Please check IP/port and try again.\n");

                joinButton.Invoke(new Action(() =>
                {
                    joinButton.Enabled = true;
                    hostButton.Enabled = true;
                    sendButton.Enabled = true;
                }));

                Close();

                // Notify Form1 to reset the client instance
                OnConnectionFailed?.Invoke();
                return;
            }

            // Update UI and proceed
            AddToChat("[Client]: Connected");

            // Enable message controls
            sendButton.Invoke(new Action(() => sendButton.Enabled = true));

            // Begin listening for incoming server messages
            clientSocket.socket.BeginReceive(clientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, clientSocket);
        }

        // Sends a plain text message to the server.
        public void SendString(string text)
        {
            if (socket == null || !socket.Connected)
            {
                AddToChat("Cannot send: not connected to server.");
                return;
            }

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }
            catch(SocketException ex)
            {
                AddToChat($"Send failed: {ex.Message}");
            }
        }

        public void Close()
        {
            if (hasDisconnected) return;

            hasDisconnected = true;
            try
            {
                if (socket != null && socket.Connected)
                    socket.Shutdown(SocketShutdown.Both);
            }
            catch { }
        }
        #endregion

        #region Networking (Receive)
        // Callback triggered when a server message is received.
        public void ReceiveCallback(IAsyncResult AR)
        {
            ClientSocket currentClientSocket = (ClientSocket)AR.AsyncState;
            int received;

            try
            {
                received = currentClientSocket.socket.EndReceive(AR);
            }
            catch (SocketException)
            {
                if (!hasDisconnected) // guard against double-fire
                {
                    hasDisconnected = true;
                    try { currentClientSocket.socket.Close(); } catch { }
                    OnDisconnected?.Invoke();
                }
                return;
            }

            // Handle graceful server disconnect (e.g. via !exit)
            if (received == 0)
            {
                if (!hasDisconnected) // guard against double-fire
                {
                    hasDisconnected = true;
                    try { socket?.Close(); } catch { }
                    OnDisconnected?.Invoke();
                }
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            string raw = Encoding.UTF8.GetString(recBuf);

            // Preserve CRLF for WinForms; trim only trailing endlines
            string norm = raw.Replace("\r\n", Environment.NewLine)
                             .TrimEnd('\r', '\n');

            // Dispatch chain (order matters for command tokens)
            if (HandleAuthMessages(norm)) { }
            else if (HandlePlayerJoin(norm)) { }
            else if (HandleBoardUpdates(norm)) { }
            else if (HandleTurnTokens(norm)) { }
            else if (HandleSessionTokens(norm)) { }
            else if (HandleSelfEchoOrChat(norm)) { }
            
            // Keep listening for more messages
            if (socket != null && socket.Connected)
            {
                currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE,
                    SocketFlags.None, ReceiveCallback, currentClientSocket);
            }

        }
        #endregion

        #region Receive Handlers

        // This handler method allows the client to register/login.
        private bool HandleAuthMessages(string norm)
        {
            if (norm.StartsWith("Registration successful!", StringComparison.OrdinalIgnoreCase))
            {
                usernameAccepted = true;
                username = norm.Replace("Registration successful! Welcome", "", StringComparison.OrdinalIgnoreCase).Trim();
                AddToChat($"[Client]: Registration successful! Welcome {username}");
                return true;
            }
            if (norm.StartsWith("Login successful!", StringComparison.OrdinalIgnoreCase))
            {
                usernameAccepted = true;
                username = norm.Replace("Login successful! Welcome back", "", StringComparison.OrdinalIgnoreCase).Trim();
                AddToChat($"[Client]: Login successful! Welcome back {username}");
                return true;
            }
            if (norm.StartsWith("Registration failed", StringComparison.OrdinalIgnoreCase))
            {
                usernameAccepted = false;
                AddToChat("[Client]: Registration failed. Username may already exist. Please try again.");
                return true;
            }
            if (norm.StartsWith("Login failed", StringComparison.OrdinalIgnoreCase))
            {
                usernameAccepted = false;
                AddToChat("[Client]: Login failed. Invalid username or password.");
                return true;
            }
            if (norm.IndexOf("Please login or register first", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AddToChat("[Client]: Please login or register first using !login or !register.");
                return true;
            }
            return false;
        }

        // This handler method allows the client to join the game !player1 / !player2.
        private bool HandlePlayerJoin(string norm)
        {
            if (norm.StartsWith("!player1", StringComparison.OrdinalIgnoreCase))
            {
                usernameAccepted = true;
                clientSocket.State = ClientState.Playing;
                clientSocket.PlayerNumber = 1;

                UI(() =>
                {
                    var f = GetForm(); if (f == null) return;
                    f.ticTacToe.playerTileType = TileType.cross;
                    f.TryEnableStartButton();
                });

                AddToChat("[Client]: You joined Tic-Tac-Toe as Player 1 (X).");
                return true;
            }

            if (norm.StartsWith("!player2", StringComparison.OrdinalIgnoreCase))
            {
                usernameAccepted = true;
                clientSocket.State = ClientState.Playing;
                clientSocket.PlayerNumber = 2;

                UI(() =>
                {
                    var f = GetForm(); if (f == null) return;
                    f.ticTacToe.playerTileType = TileType.naught;
                    f.TryEnableStartButton();
                });

                AddToChat("[Client]: You joined Tic-Tac-Toe as Player 2 (O).");
                return true;
            }

            return false;
        }

        // This handler method updates the game board by setting the tiles with X|O.
        private bool HandleBoardUpdates(string norm)
        {
            if (!norm.TrimStart().StartsWith("!settile", StringComparison.OrdinalIgnoreCase))
                return false;

            var parts = norm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3 && int.TryParse(parts[1], out int tileIndex))
            {
                TileType type = (parts[2].Equals("X", StringComparison.OrdinalIgnoreCase))
                                  ? TileType.cross
                                  : TileType.naught;

                UI(() =>
                {
                    var f = GetForm(); if (f == null) return;
                    f.ticTacToe.SetTile(tileIndex, type);
                });
            }
            return true;
        }

        // This handler method swaps the turns.
        private bool HandleTurnTokens(string norm)
        {
            if (norm.Equals("!yourturn", StringComparison.OrdinalIgnoreCase))
            {
                AddToChat("[Server]: Your turn...");
                UI(() =>
                {
                    var f = GetForm(); if (f == null) return;
                    f.ticTacToe.myTurn = true;
                    f.SetGameBoardInteractable(true);
                });
                return true;
            }

            if (norm.Equals("!waitturn", StringComparison.OrdinalIgnoreCase))
            {
                AddToChat("[Server]: Opponent's turn...");
                UI(() =>
                {
                    var f = GetForm(); if (f == null) return;
                    f.ticTacToe.myTurn = false;
                    f.SetGameBoardInteractable(false);
                });
                return true;
            }

            return false;
        }

        // This handler method deals with the game session.
        private bool HandleSessionTokens(string norm)
        {
            if (norm.Equals("!leavegame", StringComparison.OrdinalIgnoreCase))
            {
                clientSocket.State = ClientState.Chatting;
                clientSocket.PlayerNumber = 0;

                UI(() =>
                {
                    var f = GetForm(); if (f == null) return;
                    f.ticTacToe.ResetBoard();
                    f.ticTacToe.myTurn = false;
                    f.SetGameBoardInteractable(false);
                    f.StartGameButton.Enabled = false;
                });

                return true;
            }

            if (norm.IndexOf("!resetboard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                clientSocket.State = ClientState.Chatting;
                clientSocket.PlayerNumber = 0;

                AddToChat("[Server]: Players returned to lobby. Resetting Board!");
                UI(() =>
                {
                    var f = GetForm(); if (f == null) return;
                    f.ticTacToe.ResetBoard();
                    f.ticTacToe.myTurn = false;
                    f.SetGameBoardInteractable(false);
                    f.StartGameButton.Enabled = false;
                });

                return true;
            }

            return false;
        }

        // Self-echo replacement or generic chat append
        private bool HandleSelfEchoOrChat(string norm)
        {
            if (!string.IsNullOrEmpty(username) &&
                norm.StartsWith($"[{username}]:", StringComparison.Ordinal))
            {
                AddToChat(norm.Replace($"[{username}]:", "[Me]:", StringComparison.Ordinal));
                return true;
            }

            AddToChat(norm);
            return true;
        }
        #endregion
    }
}
