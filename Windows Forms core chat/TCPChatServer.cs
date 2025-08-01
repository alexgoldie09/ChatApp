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
        // Current players of the tic tac toe game
        private ClientSocket ticTacToePlayer1 = null;
        private ClientSocket ticTacToePlayer2 = null;

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
                    // Block ALL commands while playing
                    if (text.StartsWith("!", StringComparison.OrdinalIgnoreCase))
                    {
                        SendAndResume(currentClientSocket, "[Server]: Commands are disabled while you are in a game.\n");
                    }
                    else
                    {
                        // Only allow plain chat messages
                        HandleStandardMessage(currentClientSocket, text);
                    }
                    return;

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

        private void HandleJoinGame(ClientSocket client)
        {
            if (ticTacToePlayer1 == client || ticTacToePlayer2 == client)
            {
                SendAndResume(client, "[Server]: You are already in the game.\n");
                return;
            }

            if (ticTacToePlayer1 == null)
            {
                ticTacToePlayer1 = client;
                client.State = ClientState.Playing;
                SendAndResume(client, "!player1\n");
                AddToChat($"[Server]: {client.Username} joined Tic-Tac-Toe as Player 1.");
            }
            else if (ticTacToePlayer2 == null)
            {
                ticTacToePlayer2 = client;
                client.State = ClientState.Playing;
                SendAndResume(client, "!player2\n");
                AddToChat($"[Server]: {client.Username} joined Tic-Tac-Toe as Player 2.");
            }
            else
            {
                SendAndResume(client, "[Server]: Game is already full.\n");
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

            try
            {
                if (client.socket != null && client.socket.Connected)
                {
                    client.socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch { }

            try
            {
                client.socket?.Close();
            }
            catch { }

            clientSockets.Remove(client);
            client.buffer = null; // kill pending reads

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
    }
}
