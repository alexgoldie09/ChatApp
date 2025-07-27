/*
* TCPChatBase.cs
* ----------------------------------------------------------
* Shared base class for both TCPChatClient and TCPChatServer.
*
* Purpose:
* - Provides common utilities for thread-safe chat output to the UI.
* - Holds a reference to the shared TextBox used for message display.
*
* Features:
* - Thread-safe `SetChat()` to clear and replace chat content.
* - Thread-safe `AddToChat()` to append messages to chat.
* - Provides the `port` field for consistency across client/server classes.
*
* Dependencies:
* - Used by TCPChatClient.cs and TCPChatServer.cs to interact with UI.
* - Requires a Windows Forms TextBox control assigned as `chatTextBox`.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{

    public class TCPChatBase
    {
        // Reference to the UI's main chat output box (set by client/server during creation)
        public TextBox chatTextBox;

        // Port used by this client or server (set during instantiation)
        public int port;

        /// <summary>
        /// Replaces all text in the chat box with a new string, then adds a newline.
        /// Useful for initial connection attempts or reset messages.
        /// </summary>
        public void SetChat(string str)
        {
            chatTextBox.Invoke((Action)delegate
            {
                chatTextBox.Text = str;
                chatTextBox.AppendText(Environment.NewLine);
            });
        }

        /// <summary>
        /// Appends a new message to the chat box on a new line.
        /// Used to display server/client messages and logs from any thread.
        /// </summary>
        public void AddToChat(string str)
        {
            //dumb https://iandotnet.wordpress.com/tag/multithreading-how-to-update-textbox-on-gui-from-another-thread/
            chatTextBox.Invoke((Action)delegate
            {
                chatTextBox.AppendText(str);
                chatTextBox.AppendText(Environment.NewLine);
            });
        }
    }
}
