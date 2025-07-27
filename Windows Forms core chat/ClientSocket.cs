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
    public class ClientSocket
    {
        // The actual socket instance used for communication (TCP stream)
        public Socket socket;
        // The fixed buffer size for incoming data from this socket
        public const int BUFFER_SIZE = 2048;
        // The byte buffer used to store received data before processing
        public byte[] buffer = new byte[BUFFER_SIZE];
        // The username of the connected client (assigned by client commands)
        public string Username { get; set; } = null;
        // Determines if this client socket is a moderator or not (used for client commands)
        public bool IsModerator = false;
    }
}
