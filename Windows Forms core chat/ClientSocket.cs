 /*
 * ClientSocket.cs
 * ----------------------------------------------------------
 * Represents an individual client socket in the chat system.
 *
 * Purpose:
 * - Stores socket connection for a single client (host or peer).
 * - Maintains an internal receive buffer for async reads.
 * - Stores the username for the client and moderator status.
 *
 * Dependencies:
 * - Used by TCPChatServer to store connected clients in a list.
 * - Also used by TCPChatClient to hold its own socket and buffer.
 *
 */

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Windows_Forms_Chat
{
    // Defines possible states a client can be in.
    public enum ClientState
    {
        Login,      // Just connected, must login/register
        Chatting,   // Authenticated, normal chat
        Playing     // Actively in TicTacToe game
    }

    public class ClientSocket
    {
        public const int BUFFER_SIZE = 2048;
        public byte[] buffer = new byte[BUFFER_SIZE];
        public Socket socket;

        // Authentication
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;   // Store until login success

        // State management
        public ClientState State { get; set; } = ClientState.Login;
        public int PlayerNumber { get; set; } = 0; // 0 = not playing, 1 = Player1, 2 = Player2

        // Permissions
        public bool IsModerator { get; set; } = false;
    }
}
