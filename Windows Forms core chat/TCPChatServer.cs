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
                chatTextBox.Text += "Setting up server..." + Environment.NewLine;
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                serverSocket.Listen(0);
                serverSocket.BeginAccept(AcceptCallback, this);
                chatTextBox.Text += "Server setup complete." + Environment.NewLine;
            }
            catch (SocketException ex)
            {
                chatTextBox.Text += $"[Host Error]: Failed to bind to port {port}. It may already be in use.\nDetails: {ex.Message}\n";
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
            AddToChat("Client connected, waiting for request...");

            // Continue accepting
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        // Handles incoming data from a client and interprets commands.
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
                HandleDisconnect(currentClientSocket, "Client forcefully disconnected\n");
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            string text = Encoding.UTF8.GetString(recBuf).Trim();

            string[] parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts.Length > 0 ? parts[0].ToLower() : "";
            string args = parts.Length > 1 ? parts[1] : "";

            switch (cmd)
            {
                case "!username": 
                    HandleUsername(currentClientSocket, args); 
                    break;
                case "!user": 
                    HandleRename(currentClientSocket, args); 
                    break;
                case "!commands": 
                    SendAndResume(currentClientSocket, "Commands: !commands !about !user !who !whisper !roll !kick !exit\n"); 
                    break;
                case "!about": 
                    SendAndResume(currentClientSocket, "Chat Server v1.0" + Environment.NewLine + "Created by: Alexander Goldberg" + Environment.NewLine + "Purpose: Educational TCP Chat System"); 
                    break;
                case "!who": 
                    HandleWho(currentClientSocket); 
                    break;
                case "!roll": 
                    HandleRoll(currentClientSocket, args); 
                    break;
                case "!whisper": 
                    HandleWhisper(currentClientSocket, args); 
                    break;
                case "!kick": 
                    HandleKick(currentClientSocket, args); 
                    break;
                case "!exit": 
                    HandleDisconnect(currentClientSocket, "Client disconnected via !exit\n"); 
                    return;
                default: 
                    HandleStandardMessage(currentClientSocket, text); 
                    break;
            }

            if (clientSockets.Contains(currentClientSocket) && currentClientSocket.socket.Connected)
            {
                currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentClientSocket);
            }
        }

        // ---------- Command Handlers Below ----------

        private void HandleUsername(ClientSocket client, string proposed)
        {
            if (!string.IsNullOrEmpty(client.Username))
            {
                SendAndResume(client, "Use !user to rename instead.\n");
                return;
            }

            if (!IsUsernameTaken(proposed))
            {
                client.Username = proposed;
                client.socket.Send(Encoding.UTF8.GetBytes($"Username set to {proposed}\n"));
                AddToChat($"Client assigned username: {proposed}");
            }
            else
            {
                client.socket.Send(Encoding.UTF8.GetBytes($"Username '{proposed}' is taken. Disconnecting...\n"));
                HandleDisconnect(client, $"Rejected duplicate username: {proposed}\n");
            }
        }

        private void HandleRename(ClientSocket client, string newName)
        {
            if (string.IsNullOrEmpty(client.Username))
            {
                SendAndResume(client, "Set a username first using !username.\n");
                return;
            }

            if (!IsUsernameTaken(newName))
            {
                string old = client.Username;
                client.Username = newName;
                SendAndResume(client, $"Username changed to {newName}\n");
                SendToAll($"[{old}] is now known as [{newName}]\n", null);
                AddToChat($"Username change: {old} → {newName}");
            }
            else
            {
                SendAndResume(client, $"Username '{newName}' is already taken.\n");
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
            AddToChat($"Sent user list to [{client.Username}]");
        }

        private void HandleRoll(ClientSocket client, string arg)
        {
            int max = 6;
            if (!string.IsNullOrWhiteSpace(arg) && !int.TryParse(arg, out max))
            {
                SendAndResume(client, "Usage: !roll or !roll [max]\n"); return;
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
                SendAndResume(sender, "Usage:\n  !whisper \"Bob Smith\" message\n  !whisper Bob message\n");
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
                    SendAndResume(sender, "Usage:\n  !whisper \"Bob Smith\" message\n  !whisper Bob message\n");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                SendAndResume(sender, "Whisper message cannot be empty.\n");
                return;
            }

            ClientSocket targetClient = clientSockets.Find(c => c.Username == targetUsername);
            if (targetClient != null)
            {
                string senderName = sender.Username;
                string formattedMessage = $"[Whisper from {senderName}]: {message}\n";
                string confirmMessage = $"[You whispered to {targetUsername}]: {message}\n";

                targetClient.socket.Send(Encoding.UTF8.GetBytes(formattedMessage));
                SendAndResume(sender, confirmMessage);
                AddToChat($"[Whisper] {senderName} -> {targetUsername}");
            }
            else
            {
                SendAndResume(sender, $"User \"{targetUsername}\" not found.\n");
            }
        }

        private void HandleKick(ClientSocket sender, string targetName)
        {
            if (!sender.IsModerator)
            {
                SendAndResume(sender, "You do not have permission to use !kick.\n");
                return;
            }

            var target = clientSockets.Find(c => c.Username == targetName);
            if (target == null)
            {
                SendAndResume(sender, $"User \"{targetName}\" not found.\n");
                return;
            }

            if (sender == target)
            {
                SendAndResume(sender, "You cannot kick yourself.\n");
                return;
            }

            if (target.IsModerator)
            {
                SendAndResume(sender, "You cannot kick another moderator.\n");
                return;
            }

            target.socket.Send(Encoding.UTF8.GetBytes($"You were kicked by {sender.Username}.\n"));
            HandleDisconnect(target, $"{sender.Username} kicked {targetName}\n");
            SendToAll($"[{targetName}] was kicked by [{sender.Username}]\n", null);
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
            SendToAll(formatted, client);
        }

        // Disconnects a client and logs the event.
        private void HandleDisconnect(ClientSocket client, string logMessage)
        {
            if (client != null && clientSockets.Contains(client))
            {
                try
                {
                    client.socket.Shutdown(SocketShutdown.Both);
                    client.socket.Close();
                }
                catch { }

                clientSockets.Remove(client);
                AddToChat(logMessage);
            }
        }

        // Check if username is already in use.
        private bool IsUsernameTaken(string proposedUsername)
        {
            foreach (var client in clientSockets)
            {
                if (client.Username == proposedUsername)
                    return true;
            }
            return false;
        }

        // Sends a message to all clients (optionally exclude one).
        public void SendToAll(string str, ClientSocket from)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            List<ClientSocket> disconnectedClients = new List<ClientSocket>();

            foreach (ClientSocket c in clientSockets)
            {
                try
                {
                    c.socket.Send(data);
                }
                catch (SocketException)
                {
                    disconnectedClients.Add(c); // Track for removal
                }
                catch (ObjectDisposedException)
                {
                    disconnectedClients.Add(c);
                }
            }

            // Remove bad sockets
            foreach (var dc in disconnectedClients)
            {
                HandleDisconnect(dc, "Client removed due to send failure\n");
            }
        }

        // Sends a host-only message to all clients.
        public void SendHostMessage(string message)
        {
            string finalMessage = $"[Host]: {message}";
            AddToChat(finalMessage);
            SendToAll(finalMessage, null);
        }

        // Send messages to client socket and resumes receive callback.
        private void SendAndResume(ClientSocket client, string message)
        {
            try
            {
                client.socket.Send(Encoding.UTF8.GetBytes(message));
                client.socket.BeginReceive(client.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, client);
            }
            catch (SocketException)
            {
                HandleDisconnect(client, "Client send failed, disconnected\n");
            }
            catch (ObjectDisposedException)
            {
                // Already shut down
            }
        }
    }
}
