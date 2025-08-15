/*
 * TCPChatServer.cs
 * ----------------------------------------------------------
 * Handles TCP server functionality for a multi-client chat room.
 *
 * Purpose:
 * - Listens for incoming TCP client connections.
 * - Assigns, validates, and manages unique usernames.
 * - Processes incoming chat messages and command strings.
 * - Coordinates message routing, private messaging, and admin functions.
 *
 * Features:
 * - Accepts multiple concurrent TCP clients via asynchronous socket handling.
 * - Manages user identities through !username (initial) and !user (rename).
 * - Broadcasts chat messages to all connected users with formatting.
 * - Responds to core commands like !exit, !who, !about, !commands.
 * - Allows moderators (host or promoted users) to kick users with !kick
 * - Supports private messaging via !whisper [target] [message]
 * - Includes a random dice roll system via !roll and !roll [max]
 * - Detects duplicate usernames and gracefully disconnects conflicts.
 * - Uses a ClientSocket abstraction to track per-client socket, buffer, username, and moderator state.
 * - Handles socket shutdowns and client disconnections cleanly.
 *
 * Dependencies:
 * - TCPChatBase.cs (shared logic for both server/client messaging)
 * - ClientSocket.cs (per-client buffer/socket metadata and mod flags)
 * - System.Net.Sockets (asynchronous socket API)
 * - System.Windows.Forms (UI log integration for server events)
 */


using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{
    public class TCPChatServer : TCPChatBase
    {
        // Main listening socket
        public Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // All connected clients
        public List<ClientSocket> clientSockets = new List<ClientSocket>();
        // Server-side mirror of the board
        private readonly TicTacToe serverBoard = new TicTacToe();
        // Quick access to the hosting Form1 and a safe UI invoker
        private Form1 GetForm() => chatTextBox?.FindForm() as Form1;
        private void UI(Action a) { var f = GetForm(); if (f != null) f.Invoke(new Action(a)); }

        // Creates and returns a TCPChatServer instance if inputs are valid.
        public static TCPChatServer createInstance(int port, TextBox chatTextBox)
        {
            TCPChatServer tcp = null;
            if (port > 0 && port < 65535 && chatTextBox != null)
            {
                tcp = new TCPChatServer();
                tcp.port = port;
                tcp.chatTextBox = chatTextBox;
            }
            return tcp;
        }

        // Binds the socket and begins listening for new connections.
        public void SetupServer()
        {
            try
            {
                // Initialize DB when server starts
                DatabaseManager.Initialize();

                serverBoard.ResetBoard();
                UI(() =>
                {
                    var f = GetForm();
                    if (f == null) return;
                    f.ticTacToe.ResetBoard();
                    f.SetGameBoardInteractable(false); // keep disabled
                                                       // idle = gray
                    foreach (var btn in f.ticTacToe.buttons) btn.BackColor = System.Drawing.Color.Gray;
                });

                chatTextBox.Text += "[Server]: Setting up server..." + Environment.NewLine;
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                serverSocket.Listen(0);
                serverSocket.BeginAccept(AcceptCallback, this);
                chatTextBox.Text += "[Server]: Server setup complete." + Environment.NewLine;
            }
            catch (SocketException ex)
            {
                chatTextBox.Text += $"[Server Error]: Failed to bind to port {port}. It may already be in use.\nDetails: {ex.Message}\n";
                throw;
            }
        }

        // Cleanly shuts down all client and server sockets.
        public void CloseAllSockets()
        {
            foreach (ClientSocket clientSocket in clientSockets)
            {
                clientSocket.socket.Shutdown(SocketShutdown.Both);
                clientSocket.socket.Close();
            }
            clientSockets.Clear();
            serverSocket.Close();
        }

        // Accepts a new client and starts receiving their data.
        public void AcceptCallback(IAsyncResult AR)
        {
            Socket joiningSocket;

            try
            {
                joiningSocket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            ClientSocket newClientSocket = new ClientSocket();
            newClientSocket.socket = joiningSocket;

            clientSockets.Add(newClientSocket);
            joiningSocket.BeginReceive(newClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, newClientSocket);
            AddToChat("[Server]: Client connected, waiting for request...");

            // Continue accepting
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        // Handles incoming data from a client and interprets commands.
        public void ReceiveCallback(IAsyncResult AR)
        {
            ClientSocket currentClientSocket = (ClientSocket)AR.AsyncState;

            // Guard: client already disconnected/removed
            if (currentClientSocket == null ||
                currentClientSocket.buffer == null ||
                !clientSockets.Contains(currentClientSocket))
            {
                return;
            }

            int received;

            if (clientSockets.Contains(currentClientSocket))
            {
                try
                {
                    received = currentClientSocket.socket.EndReceive(AR);
                }
                catch (SocketException)
                {
                    HandleDisconnect(currentClientSocket, "[Server]: Client forcefully disconnected\n");
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
            else
            {
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            string text = Encoding.UTF8.GetString(recBuf).Trim();

            // Guard: empty input
            if (string.IsNullOrWhiteSpace(text))
            {
                SendAndResume(currentClientSocket, "Empty command ignored.\n");
                return;
            }

            // Handle by state
            switch (currentClientSocket.State)
            {
                case ClientState.Login:
                    var (cmdLogin, argsLogin) = ParseCommand(text);

                    if (cmdLogin == "!register")
                    {
                        HandleRegister(currentClientSocket, argsLogin);
                    }
                    else if (cmdLogin == "!login")
                    {
                        HandleLogin(currentClientSocket, argsLogin);
                    }
                    else
                    {
                        SendAndResume(currentClientSocket, "Please login or register first using !login or !register.\n");
                    }
                    return;

                case ClientState.Chatting:
                    var (cmd, args) = ParseCommand(text);

                    switch (cmd)
                    {
                        case "!user": HandleRename(currentClientSocket, args); break;
                        case "!commands": SendAndResume(currentClientSocket, "Commands: !commands !about !user !who !whisper !roll !join !kick !exit\n"); break;
                        case "!about": SendAndResume(currentClientSocket, "Chat Server v1.0" + Environment.NewLine + "Created by: Alexander Goldberg" + Environment.NewLine + "Purpose: Educational TCP Chat System"); break;
                        case "!who": HandleWho(currentClientSocket); break;
                        case "!roll": HandleRoll(currentClientSocket, args); break;
                        case "!whisper": HandleWhisper(currentClientSocket, args); break;
                        case "!kick": HandleKick(currentClientSocket, args); break;
                        case "!join": HandleJoinGame(currentClientSocket); break;
                        case "!exit": HandleDisconnect(currentClientSocket, $"[Server]: {currentClientSocket.Username} disconnected via !exit\n"); return;
                        default: HandleStandardMessage(currentClientSocket, text); break;
                    }
                    break;

                case ClientState.Playing:
                    if (text.StartsWith("!", StringComparison.OrdinalIgnoreCase))
                    {
                        var (cmdPlay, argsPlay) = ParseCommand(text);

                        if (cmdPlay == "!whisper") HandleWhisper(currentClientSocket, argsPlay);
                        else if (cmdPlay == "!exit") HandleDisconnect(currentClientSocket, $"[Server]: {currentClientSocket.Username} exited the game and disconnected.\n");
                        else if (cmdPlay == "!startgame") HandleStartGame(currentClientSocket);
                        else if (cmdPlay == "!move") HandleMove(currentClientSocket, argsPlay);
                        else SendAndResume(currentClientSocket, "[Server]: Only !whisper, !exit, !startgame, or !move allowed during a game.\n");
                    }
                    else
                    {
                        HandleStandardMessage(currentClientSocket, text);
                    }
                    break;

            }

            if (clientSockets.Contains(currentClientSocket) && currentClientSocket.socket.Connected)
            {
                currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentClientSocket);
            }
        }

        #region Handler Methods
        // ---------- Command Handlers Below ----------
        private void HandleRegister(ClientSocket client, string args)
        {
            var creds = ParseCredentials(args);
            if (creds == null)
            {
                SendAndResume(client, "[Server] Usage - !register <username> <password>\n");
                return;
            }

            var (username, password) = creds.Value;

            // Validate username rules
            if (!DatabaseManager.ValidateUsername(username, out string error))
            {
                SendAndResume(client, $"[Server]: Registration failed. {error}\n");
                return;
            }

            // Check if already connected (case-insensitive)
            if (IsUserAlreadyConnected(username))
            {
                SendAndResume(client, $"[Server]: Registration failed. User '{username}' is already logged in.\n");
                return;
            }

            if (DatabaseManager.TryRegister(username, password, out string dbError))
            {
                // Store chosen display casing in memory
                client.Username = username.Trim();
                client.Password = password;
                client.State = ClientState.Chatting;

                SendAndResume(client, $"Registration successful! Welcome {client.Username}\n");
                AddToChat($"[Server]: {client.Username} registered and logged in.");
            }
            else
            {
                SendAndResume(client, $"[Server]: {dbError}\n");
            }
        }

        private void HandleLogin(ClientSocket client, string args)
        {
            var creds = ParseCredentials(args);
            if (creds == null)
            {
                SendAndResume(client, "[Server] Usage - !login <username> <password>\n");
                return;
            }

            var (username, password) = creds.Value;

            // Validate format only (not existence yet)
            if (!DatabaseManager.ValidateUsername(username, out string error))
            {
                SendAndResume(client, $"[Server]: Login failed. {error}\n");
                return;
            }

            // First check DB (returns stored displayName)
            if (DatabaseManager.TryLogin(username, password, out string dbError, out string displayName))
            {
                // Then check if already connected (case-insensitive)
                if (IsUserAlreadyConnected(displayName))
                {
                    SendAndResume(client, $"[Server]: Login failed. User '{displayName}' is already logged in.\n");
                    return;
                }

                client.Username = displayName; // always the DB's display version
                client.Password = password;
                client.State = ClientState.Chatting;

                SendAndResume(client, $"Login successful! Welcome back {displayName}\n");
                AddToChat($"[Server]: {displayName} logged in.");
            }
            else
            {
                SendAndResume(client, $"[Server]: {dbError}\n");
            }
        }

        private void HandleRename(ClientSocket client, string newName)
        {
            if (string.IsNullOrEmpty(client.Username))
            {
                SendAndResume(client, "[Server]: You must be logged in to change your username.\n");
                return;
            }

            if (!DatabaseManager.ValidateUsername(newName, out string error))
            {
                SendAndResume(client, $"[Server]: Rename failed. {error}\n");
                return;
            }

            if (IsUsernameTaken(newName))
            {
                SendAndResume(client, $"[Server]: Username '{newName}' is already taken.\n");
                return;
            }

            if (DatabaseManager.TryUpdateUsername(client.Username, newName, out string dbError))
            {
                string old = client.Username;
                client.Username = newName.Trim(); // update to new display version

                SendAndResume(client, $"[Server]: Username changed to {client.Username}\n");
                SendToAll($"[{old}] is now known as [{client.Username}]\n", null);
                AddToChat($"[Server]: Username changed from {old} → {client.Username}");
            }
            else
            {
                SendAndResume(client, $"[Server]: Rename failed. {dbError}\n");
            }
        }

        private void HandleWho(ClientSocket client)
        {
            StringBuilder list = new StringBuilder("Connected users:");
            bool first = true;
            foreach (var c in clientSockets)
            {
                if (!string.IsNullOrWhiteSpace(c.Username))
                {
                    list.Append(first ? " " : ", ");
                    list.Append(c.Username);
                    first = false;
                }
            }
            SendAndResume(client, list.ToString() + "\n");
            AddToChat($"[Server]: Sent user list to [{client.Username}]");
        }

        private void HandleRoll(ClientSocket client, string arg)
        {
            int max = 6;
            if (!string.IsNullOrWhiteSpace(arg) && !int.TryParse(arg, out max))
            {
                SendAndResume(client, "[Server]: Usage - !roll or !roll [max]\n"); return;
            }

            if (max < 1)
            {
                SendAndResume(client, "Roll must be >= 1.\n"); return;
            }

            int result = new Random().Next(1, max + 1);
            string message = $"{client.Username} rolled a {result} (1 – {max})";
            AddToChat($"[Roll] {message}");
            SendToAll($"[Roll] {message}\n", null);
        }

        private void HandleWhisper(ClientSocket sender, string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                SendAndResume(sender, "[Server]: Usage -\n !whisper \"Bob Smith\" message\n  !whisper Bob message\n");
                return;
            }

            string targetUsername = null;
            string message = null;

            if (args.StartsWith("\""))
            {
                // Extract quoted name
                int endQuote = args.IndexOf("\"", 1);
                if (endQuote != -1)
                {
                    targetUsername = args.Substring(1, endQuote - 1).Trim();
                    message = args.Substring(endQuote + 1).Trim();
                }
                else
                {
                    SendAndResume(sender, "Improper whisper command. Missing closing quote.\n");
                    return;
                }
            }
            else
            {
                // Parse first word as username, rest as message
                var parts = args.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    targetUsername = parts[0].Trim();
                    message = parts[1].Trim();
                }
                else
                {
                    SendAndResume(sender, "[Server]: Usage -\n  !whisper \"Bob Smith\" message\n  !whisper Bob message\n");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                SendAndResume(sender, "Whisper message cannot be empty.\n");
                return;
            }

            // Case-insensitive lookup
            string targetLower = targetUsername.ToLowerInvariant();
            ClientSocket targetClient = clientSockets.Find(c =>
                !string.IsNullOrEmpty(c.Username) && c.Username.ToLowerInvariant() == targetLower);

            if (targetClient != null)
            {
                string senderName = sender.Username;
                string formattedMessage = $"[Whisper from {senderName}]: {message}\n";
                string confirmMessage = $"[You whispered to {targetClient.Username}]: {message}\n";

                targetClient.socket.Send(Encoding.UTF8.GetBytes(formattedMessage));
                SendAndResume(sender, confirmMessage);
                AddToChat($"[Server]: {senderName} whispered {targetClient.Username}");
            }
            else
            {
                SendAndResume(sender, $"[Server]: User \"{targetUsername}\" not found.\n");
            }
        }

        private void HandleKick(ClientSocket sender, string targetName)
        {
            if (!sender.IsModerator)
            {
                SendAndResume(sender, "[Server]: You do not have permission to use !kick.\n");
                return;
            }

            // Case-insensitive lookup
            string targetLower = targetName.ToLowerInvariant();
            var target = clientSockets.Find(c =>
                !string.IsNullOrEmpty(c.Username) && c.Username.ToLowerInvariant() == targetLower);

            if (target == null)
            {
                SendAndResume(sender, $"[Server]: User \"{targetName}\" not found.\n");
                return;
            }

            if (sender == target)
            {
                SendAndResume(sender, "[Server]: You cannot kick yourself.\n");
                return;
            }

            if (target.IsModerator)
            {
                SendAndResume(sender, "[Server]: You cannot kick another moderator.\n");
                return;
            }

            target.socket.Send(Encoding.UTF8.GetBytes($"You were kicked by {sender.Username}.\n"));
            HandleDisconnect(target, $"{sender.Username} kicked {target.Username}\n");
            SendToAll($"[{target.Username}] was kicked by [{sender.Username}]\n", null);
        }

        private void HandleStandardMessage(ClientSocket client, string text)
        {
            if (string.IsNullOrWhiteSpace(client.Username))
            {
                client.socket.Send(Encoding.UTF8.GetBytes("Set a username before sending messages.\n"));
                HandleDisconnect(client, "Disconnected for no username\n");
                return;
            }

            string formatted = $"[{client.Username}]: {text}";
            AddToChat(formatted);

            // Send to everyone including sender
            SendToAll(formatted, null);
        }

        // Disconnects a client and logs the event (idempotent).
        private void HandleDisconnect(ClientSocket client, string logMessage)
        {
            if (client == null) return;

            if (!clientSockets.Contains(client))
                return;

            //try
            //{
            //    if (client.socket != null && client.socket.Connected)
            //    {
            //        client.socket.Shutdown(SocketShutdown.Both);
            //    }
            //}
            //catch { }

            try
            {
                bool hasBeenDisconnected = false;
                if (client.socket != null && client.socket.Connected)
                {
                    if (!hasBeenDisconnected)
                    {
                        hasBeenDisconnected = true;
                        client.socket.Close();
                    }
                }
            }
            catch { }

            clientSockets.Remove(client);
            client.buffer = null;

            if (client.State == ClientState.Playing)
            {
                bool wasInGame = false;
                string username = client.Username;

                if (GameStateManager.GetPlayer1() == username)
                {
                    GameStateManager.ClearPlayer1();
                    wasInGame = true;
                }

                if (GameStateManager.GetPlayer2() == username)
                {
                    GameStateManager.ClearPlayer2();
                    wasInGame = true;
                }

                if (GameStateManager.GetCurrentTurn() == username)
                {
                    GameStateManager.ClearCurrentTurn();
                    wasInGame = true;               
                }

                client.State = ClientState.Login;
                client.PlayerNumber = 0;

                if (wasInGame)
                {
                    SendToAll($"[Server]: {username} left the Tic-Tac-Toe game.\n", null);
                }

                // Reset if either slot is now empty
                if (string.IsNullOrEmpty(GameStateManager.GetPlayer1()) ||
    string.IsNullOrEmpty(GameStateManager.GetPlayer2()))
                {
                    // find any remaining player to pop out of Playing
                    string remaining = GameStateManager.GetPlayer1();
                    if (string.IsNullOrEmpty(remaining))
                        remaining = GameStateManager.GetPlayer2();

                    GameStateManager.ResetGame();
                    SendToAll("!resetboard\n", null);

                    AddToChat("[Server]: Game reset due to a player leaving.");
                    serverBoard.ResetBoard();
                    UI(() =>
                    {
                        var f = GetForm(); if (f == null) return;
                        f.ticTacToe.ResetBoard();
                        f.SetGameBoardInteractable(false);
                        foreach (var btn in f.ticTacToe.buttons) btn.BackColor = System.Drawing.Color.Gray;
                    });

                    if (!string.IsNullOrEmpty(remaining))
                    {
                        var other = clientSockets.Find(c => c.Username == remaining);
                        if (other != null)
                        {
                            other.State = ClientState.Chatting;
                            other.PlayerNumber = 0;
                            SendAndResume(other, "!leavegame\n");
                        }
                    }
                }
            }

            AddToChat(logMessage);
        }
        #endregion

        #region Utility Methods
        // Check if username is already taken by a connected client
        private bool IsUsernameTaken(string proposedUsername)
        {
            string lower = proposedUsername.ToLowerInvariant();
            foreach (var client in clientSockets)
            {
                if (!string.IsNullOrEmpty(client.Username) &&
                    client.Username.ToLowerInvariant() == lower)
                {
                    return true;
                }
            }
            return false;
        }

        // Check if user is already connected to server
        private bool IsUserAlreadyConnected(string username)
        {
            string lower = username.ToLowerInvariant();
            foreach (var c in clientSockets)
            {
                if (!string.IsNullOrEmpty(c.Username) &&
                    c.Username.ToLowerInvariant() == lower)
                {
                    return true;
                }
            }
            return false;
        }

        // Split command and args safely
        private (string cmd, string args) ParseCommand(string input)
        {
            string[] parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts.Length > 0 ? parts[0].ToLower() : "";
            string args = parts.Length > 1 ? parts[1] : "";
            return (cmd, args);
        }

        // Split username + password (for login/register)
        private (string user, string pass)? ParseCredentials(string input)
        {
            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
                return (parts[0].Trim(), parts[1].Trim());
            return null;
        }
        private string MarkerFor(string user)
        {
            return GameStateManager.IsPlayer1(user) ? "X" : "O";
        }

        private void LogTurnOf(string user)
        {
            AddToChat($"[Turn] {user} ({MarkerFor(user)}) to move.");
        }
        #endregion

        #region Send Methods
        // Sends a message to all clients (optionally exclude one).
        public void SendToAll(string str, ClientSocket from)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            List<ClientSocket> disconnectedClients = new List<ClientSocket>();

            foreach (ClientSocket c in clientSockets.ToArray()) // snapshot for safety
            {
                if (from != null && c == from)
                    continue; // skip sender if needed

                try
                {
                    if (c.socket != null && c.socket.Connected)
                    {
                        c.socket.Send(data);
                    }
                    else
                    {
                        disconnectedClients.Add(c);
                    }
                }
                catch (SocketException)
                {
                    disconnectedClients.Add(c); // only this client fails
                }
                catch (ObjectDisposedException)
                {
                    disconnectedClients.Add(c);
                }
            }

            // Remove only the broken ones
            foreach (var dc in disconnectedClients)
            {
                HandleDisconnect(dc, "[Server]: Client removed due to send failure\n");
            }
        }

        // Sends a host-only message to all clients.
        public void SendHostMessage(string message)
        {
            string finalMessage = $"[Server]: {message}";
            AddToChat(finalMessage);
            SendToAll(finalMessage, null);
        }

        // Send messages to client socket and resumes receive callback.
        private void SendAndResume(ClientSocket client, string message)
        {
            try
            {
                if (client.socket == null || !client.socket.Connected)
                    return; // already disconnected

                client.socket.Send(Encoding.UTF8.GetBytes(message));

                // Only resume if client is still in list
                if (clientSockets.Contains(client) && client.buffer != null)
                {
                    client.socket.BeginReceive(client.buffer, 0, ClientSocket.BUFFER_SIZE,
                        SocketFlags.None, ReceiveCallback, client);
                }
            }
            catch (SocketException)
            {
                HandleDisconnect(client, "[Server]: Client send failed, disconnected\n");
            }
            catch (ObjectDisposedException)
            {
                // already cleaned up
            }
        }
        #endregion

        #region Game Board Methods
        private void HandleJoinGame(ClientSocket client)
        {
            string username = client.Username;

            if (client.State == ClientState.Playing)
            {
                SendAndResume(client, "[Server]: You are already in the game.\n");
                return;
            }

            if (GameStateManager.IsPlayer(username))
            {
                SendAndResume(client, "[Server]: You already joined the game.\n");
                return;
            }

            string player1 = GameStateManager.GetPlayer1();
            string player2 = GameStateManager.GetPlayer2();

            if (!string.IsNullOrEmpty(player1) && !string.IsNullOrEmpty(player2))
            {
                SendAndResume(client, "[Server]: Game is already full.\n");
                return;
            }

            if (string.IsNullOrEmpty(player1))
            {
                GameStateManager.SetPlayer1(username);
                client.State = ClientState.Playing;
                client.PlayerNumber = 1;
                SendAndResume(client, "!player1\n");
                AddToChat($"[Server]: {username} joined Tic-Tac-Toe as Player 1.");
            }
            else
            {
                GameStateManager.SetPlayer2(username);
                client.State = ClientState.Playing;
                client.PlayerNumber = 2;
                SendAndResume(client, "!player2\n");
                AddToChat($"[Server]: {username} joined Tic-Tac-Toe as Player 2.");
            }
            // After sending !player1 or !player2
            AddToChat($"[Server]: {username} is {(client.PlayerNumber == 1 ? "X (Player 1)" : "O (Player 2)")}");

        }

        private void HandleStartGame(ClientSocket client)
        {
            if (!GameStateManager.IsPlayer1(client.Username))
            {
                SendAndResume(client, "[Server]: Only Player 1 can start the game.\n");
                return;
            }

            if (!GameStateManager.CanStartGame())
            {
                SendAndResume(client, "[Server]: Both players must be present to start the game.\n");
                return;
            }

            // Reset mirror board for a fresh match
            serverBoard.ResetBoard();
            UI(() =>
            {
                var f = GetForm(); if (f == null) return;
                f.ticTacToe.ResetBoard();
                f.SetGameBoardInteractable(false); // never interactable on server
                                                   // active game = violet
                foreach (var btn in f.ticTacToe.buttons) btn.BackColor = System.Drawing.Color.Violet;
            });

            GameStateManager.SetCurrentTurn(client.Username);

            SendToAll("[Server]: Game has started.\n", null);
            SendAndResume(client, "!yourturn");

            // Announce whose turn it is (server log)
            LogTurnOf(client.Username);

            string opponent = GameStateManager.IsPlayer1(client.Username)
                ? GameStateManager.GetPlayer2()
                : GameStateManager.GetPlayer1();

            ClientSocket opponentSocket = clientSockets.Find(c => c.Username == opponent);
            if (opponentSocket != null)
            {
                SendAndResume(opponentSocket, "!waitturn");
            }
        }

        private void HandleMove(ClientSocket client, string args)
        {
            string username = client.Username;
            if (GameStateManager.GetCurrentTurn() != username)
            {
                SendAndResume(client, "[Server]: Not your turn.\n");
                return;
            }

            if (!int.TryParse(args, out int index) || index < 0 || index > 8)
            {
                SendAndResume(client, "[Server]: Invalid move index.\n");
                return;
            }

            TileType tile = GameStateManager.IsPlayer1(username) ? TileType.cross : TileType.naught;

            if (!GameStateManager.SetTile(index, tile))
            {
                SendAndResume(client, "[Server]: Tile already taken.\n");
                return;
            }

            // Mirror the move on the server board and UI
            serverBoard.SetTile(index, tile);
            UI(() =>
            {
                var f = GetForm(); if (f == null) return;
                f.ticTacToe.SetTile(index, tile);
                f.SetGameBoardInteractable(false); // ensure disabled
                                                   // keep violet while game is active
                foreach (var btn in f.ticTacToe.buttons) btn.BackColor = System.Drawing.Color.Violet;
            });

            // Server log for the move
            AddToChat($"[Move] {username} placed {(tile == TileType.cross ? "X" : "O")} at {index}.");


            SendToAll($"!settile {index} {(tile == TileType.cross ? "X" : "O")}", null);

            GameState result = GameStateManager.GetGameState();

            if (result == GameState.playing)
            {
                string next = GameStateManager.IsPlayer1(username)
                    ? GameStateManager.GetPlayer2()
                    : GameStateManager.GetPlayer1();

                GameStateManager.SetCurrentTurn(next);

                var currentPlayer = clientSockets.Find(c => c.Username == next);
                var waitPlayer = clientSockets.Find(c => c.Username != next && GameStateManager.IsPlayer(c.Username));

                if (currentPlayer != null) SendAndResume(currentPlayer, "!yourturn");
                if (waitPlayer != null) SendAndResume(waitPlayer, "!waitturn");
                // Server log for whose turn is next
                LogTurnOf(next);
            }
            else
            {
                // Figure out outcome text AND update database scores
                string p1 = GameStateManager.GetPlayer1(); // Player 1 is X
                string p2 = GameStateManager.GetPlayer2(); // Player 2 is O

                // Private result lines to players with their records
                var p1Sock = clientSockets.Find(c => c.Username == p1);
                var p2Sock = clientSockets.Find(c => c.Username == p2);

                // Build a precise end message (covers draw explicitly)
                string endMsg;
                switch (result)
                {
                    case GameState.crossWins:
                        endMsg = "X wins!";
                        if (!string.IsNullOrEmpty(p1)) DatabaseManager.IncrementWins(p1);
                        if (!string.IsNullOrEmpty(p2)) DatabaseManager.IncrementLosses(p2);
                        break;

                    case GameState.naughtWins:
                        endMsg = "O wins!";
                        if (!string.IsNullOrEmpty(p2)) DatabaseManager.IncrementWins(p2);
                        if (!string.IsNullOrEmpty(p1)) DatabaseManager.IncrementLosses(p1);
                        break;

                    case GameState.draw:
                        endMsg = "It's a draw!";
                        if (!string.IsNullOrEmpty(p1)) DatabaseManager.IncrementDraws(p1);
                        if (!string.IsNullOrEmpty(p2)) DatabaseManager.IncrementDraws(p2);
                        break;

                    default:
                        endMsg = "No results.";
                        break;
                }

                // Get fresh records for both players (safe if either is null)
                var p1Stats = string.IsNullOrEmpty(p1) ? (0, 0, 0) : DatabaseManager.GetStats(p1);
                var p2Stats = string.IsNullOrEmpty(p2) ? (0, 0, 0) : DatabaseManager.GetStats(p2);

                if (p1Sock != null)
                {
                    string you =
                        result == GameState.crossWins ? "You won!" :
                        result == GameState.naughtWins ? "You lost." :
                        result == GameState.draw ? "Draw." : "Game over.";
                    SendAndResume(p1Sock, $"[Result]: {you} Your record: {p1Stats.Item1} wins/{p1Stats.Item2} losses/{p1Stats.Item3} draws.\n");
                }
                if (p2Sock != null)
                {
                    string you =
                        result == GameState.naughtWins ? "You won!" :
                        result == GameState.crossWins ? "You lost." :
                        result == GameState.draw ? "Draw." : "Game over.";
                    SendAndResume(p2Sock, $"[Result]: {you} Your record: {p2Stats.Item1} wins/{p2Stats.Item2} losses/{p2Stats.Item3} draws.\n");
                }

                // Inform room and clients with detailed results
                SendToAll($"[Game Over]: {endMsg}\n", null);
                SendToAll("!resetboard\n", null);

                // Server log (also include both records)
                AddToChat($"[Game Over]: {endMsg}");
                if (!string.IsNullOrEmpty(p1)) AddToChat($"[Record] {p1}: {p1Stats.Item1} wins/{p1Stats.Item2} losses/{p1Stats.Item3} draws");
                if (!string.IsNullOrEmpty(p2)) AddToChat($"[Record] {p2}: {p2Stats.Item1} wins/{p2Stats.Item2} losses/{p2Stats.Item3} draws");
                AddToChat("[Server]: Game finished. Resetting board and returning players to lobby.");

                // Update server UI board (gray idle)
                UI(() =>
                {
                    var f = GetForm(); if (f == null) return;
                    f.ticTacToe.ResetBoard();
                    f.SetGameBoardInteractable(false);
                    foreach (var btn in f.ticTacToe.buttons) btn.BackColor = System.Drawing.Color.Gray;
                });

                // Drop both back to Chatting (your existing pattern)
                foreach (var c in clientSockets)
                {
                    if (!string.IsNullOrEmpty(c.Username) && (c.Username == p1 || c.Username == p2))
                    {
                        c.State = ClientState.Chatting;
                        c.PlayerNumber = 0;
                        SendAndResume(c, "!leavegame\n");
                    }
                }

                // Reset central state
                GameStateManager.ResetGame();
            }
        }

        #endregion
    }
}
