/*
 * TCPChatClient.cs
 * ----------------------------------------------------------
 * Handles TCP client functionality for joining a chat server.
 *
 * Purpose:
 * - Establishes a TCP connection to a specified remote chat server.
 * - Manages sending and receiving messages from the server.
 * - Processes command-based messages (e.g., !username, !user, !exit).
 *
 * Features:
 * - Connection retry with 10 attempts.
 * - Manual username setup through !username command after connection.
 * - Automatically updates UI buttons based on connection state.
 * - Replaces own messages with [Me] instead of [Username].
 * - Handles graceful disconnection and rejection feedback.
 *
 * Dependencies:
 * - Inherits from TCPChatBase.cs (for shared AddToChat, SetChat).
 * - Uses ClientSocket.cs to store buffer/socket.
 * - Communicates with TCPChatServer.cs logic via command strings.
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{
    public class TCPChatClient : TCPChatBase
    {
        // Main listening socket
        public Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // User's socket
        public ClientSocket clientSocket = new ClientSocket();

        // Reference to the server port and IP to be used
        public int serverPort;
        public string serverIP;

        // Reference to the username and check to username being accepted
        public bool usernameAccepted = false;
        public string username;

        // Callbacks to inform Form1 of events
        public Action OnUsernameRejected;
        public Action OnConnectionFailed;

        // Factory method to validate and create a TCPChatClient instance.
        public static TCPChatClient CreateInstance(int port, int serverPort, string serverIP, TextBox chatTextBox)
        {
            TCPChatClient tcp = null;

            // Sanity check before allocating
            if (port > 0 && port < 65535 &&
                serverPort > 0 && serverPort < 65535 &&
                serverIP.Length > 0 &&
                chatTextBox != null)
            {
                tcp = new TCPChatClient();
                tcp.port = port;
                tcp.serverPort = serverPort;
                tcp.serverIP = serverIP;
                tcp.chatTextBox = chatTextBox;
                tcp.clientSocket.socket = tcp.socket;
            }

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
                    SetChat("Connection attempt " + attempts);
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
                AddToChat("Unable to connect to server after 10 attempts. Please check IP/port and try again.\n");

                // Re-enable UI and inform user
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
            AddToChat("Connected");
            AddToChat("To start, type !username YourName and press Send.");

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
                // Block repeated use of !username after initial acceptance
                if (text.StartsWith("!username ", StringComparison.OrdinalIgnoreCase))
                {
                    if (usernameAccepted)
                    {
                        AddToChat("You have already set a username. Use !user [new_name] to change it.");
                        return;
                    }
                }

                byte[] buffer = Encoding.UTF8.GetBytes(text);
                socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }
            catch(SocketException ex)
            {
                AddToChat($"Send failed: {ex.Message}");
            }
        }

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
                AddToChat("Client forcefully disconnected");
                currentClientSocket.socket.Close();
                return;
            }

            // Handle graceful server disconnect (e.g. via !exit)
            if (received == 0)
            {
                AddToChat("Disconnected from server.");
                socket.Close();

                chatTextBox.Invoke(new Action(() =>
                {
                    if (chatTextBox.FindForm() is Form1 form)
                    {
                        form.JoinButton.Enabled = true;
                        form.HostButton.Enabled = true;
                        form.SendButton.Enabled = false;
                        form.client = null; // Reset for fresh connect
                    }
                }));

                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            string text = Encoding.UTF8.GetString(recBuf);

            // Username accepted by server
            if (text.StartsWith("Username set to"))
            {
                usernameAccepted = true;
                this.username = text.Substring("Username set to".Length).Trim();

                chatTextBox.Invoke(new Action(() =>
                {
                    if (chatTextBox.FindForm() is Form1 form)
                    {
                        form.SendButton.Enabled = true;
                    }
                }));
            }
            // Username changed
            else if (text.StartsWith("Username changed to "))
            {
                this.username = text.Substring("Username changed to ".Length).Trim();
            }
            // Username rejected
            else if (text.Contains("is already in use"))
            {
                usernameAccepted = false;
                AddToChat("Username rejected by server. Disconnecting...");
                Close();
                OnUsernameRejected?.Invoke();
                return;
            }
            // No username enforcement message
            else if (text.Contains("You must set a username before sending messages"))
            {
                AddToChat("Disconnected: You must set a username using !username before chatting.");
                Close();
                return;
            }

            // Show message, replacing your own username with [Me]
            if (!string.IsNullOrEmpty(username) && text.StartsWith($"[{username}]:"))
            {
                AddToChat(text.Replace($"[{username}]:", "[Me]:"));
            }
            else
            {
                AddToChat(text);
            }

            // Keep listening for more messages
            currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentClientSocket);
        }

        // Request to change the client's username (triggers server logic)
        public void RequestUsernameChange(string newUsername)
        {
            SendString("!user " + newUsername);
        }

        // Close the underlying socket connection
        public void Close()
        {
            socket.Close();
        }
    }
}
