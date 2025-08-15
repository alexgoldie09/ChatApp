/*
* TCPChatBase.cs
* ----------------------------------------------------------
* Shared base class for both TCPChatClient and TCPChatServer.
*
* Purpose:
* - Provides common utilities for thread-safe chat output to the UI.
* - Holds a reference to the shared TextBox used for message display.
* - Centralizes small helpers for parsing and newline handling.
*
* Features:
* - Thread-safe SetChat/AddToChat that normalize line breaks for WinForms.
* - UI invoker and Form accessor for safe cross-thread UI updates.
* - Command parsing helper returning (cmd, args).
* - EnsureProtocolNewline for wire messages (guarantees trailing '\n').
*/

using System;
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

        #region Shared helpers

        // Grab the hosting form (if available)
        protected Form1 GetForm() => chatTextBox?.FindForm() as Form1;

        // UI invoker for safe cross-thread calls
        protected void UI(Action a)
        {
            var f = GetForm();
            if (f != null) f.Invoke(new Action(a));
        }

        // Normalize and append to the chat box (preserve CRLF visually)
        public void AddToChat(string str)
        {
            if (chatTextBox == null || str == null) return;
            chatTextBox.Invoke((Action)delegate
            {
                // Ensure any '\n' render correctly in WinForms
                chatTextBox.AppendText(str.Replace("\n", Environment.NewLine));
                // Make sure each AddToChat ends on a new line
                if (!str.EndsWith("\n")) chatTextBox.AppendText(Environment.NewLine);
            });
        }

        // Replace all text in the chat box, then add a newline
        public void SetChat(string str)
        {
            if (chatTextBox == null) return;
            chatTextBox.Invoke((Action)delegate
            {
                chatTextBox.Text = (str ?? string.Empty).Replace("\n", Environment.NewLine);
                chatTextBox.AppendText(Environment.NewLine);
            });
        }

        // Consistent command parsing for both client/server
        protected (string cmd, string args) ParseCommand(string input)
        {
            string[] parts = (input ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
            string args = parts.Length > 1 ? parts[1] : "";
            return (cmd, args);
        }

        // Ensure protocol newline for multi-line messages over the wire
        protected static string EnsureProtocolNewline(string msg)
            => string.IsNullOrEmpty(msg) ? "\n" : (msg.EndsWith("\n") ? msg : msg + "\n");

        #endregion
    }
}
