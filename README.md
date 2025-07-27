# TCP Chat Server

A multiclient TCP chat server written in C#. This project demonstrates socket-based networking using asynchronous communication patterns, allowing multiple clients to connect, chat, and interact via commands in real-time.

## 🚀 Features

- Host a TCP chat server on a specified port.
- Supports multiple clients with unique usernames.
- Private messaging via `!whisper`.
- Moderator-based kicking system (`!kick`).
- Command list with `!commands`, `!who`, `!about`, and `!roll`.
- Socket-based networking using `System.Net.Sockets`.
- UI output via `System.Windows.Forms.TextBox`.

## 🛠️ How to Run

1. Open the solution in Visual Studio.
2. Build and run the project.
3. Optionally open at "A2_C#_Chat_App\Windows Forms core chat\bin\Debug\netcoreapp3.1\Windows Forms core chat.exe".

### Server

1. Enter the desired port number in the "My Port" field (filled out by default).
2. Press the “Host Server” button to begin hosting on that desired port.
3. Once connected, begin sending commands/messages.

Server can:

- Send message to all clients.
- Receive feedback and display messages from client usage.
- Uses supported commands to interact with others.

### 🛠️ Host-Only Commands

| Command              | Description                                                          |
|----------------------|----------------------------------------------------------------------|
| `!mod Alex`          | Toggles moderator status for the user "Alex".                        |
| `!mods`              | Lists all current moderators (output shown only in the host UI).     |
| `!kick Alex`         | Kicks the user "Alex" from the server immediately.                   |


### Client

1. Enter the matching port number to the server in the "Server Port" field (filled out by default).
2. Enter the server IP address in the "Server IP" field (filled out by default, NOTE: 127.0.0.1 is localhost).
3. Press the “Join Server” button to connect to the server.
4. Once connected, begin sending commands/messages.

Each client must:

- Send `!username YourName` upon connection.
- Uses supported commands to interact with others.

### 💬 Client Commands

| Command                    | Description                                                              |
|----------------------------|--------------------------------------------------------------------------|
| `!username Alex`           | Sets your initial username (must be unique).                             |
| `!user Alex2`              | Changes your username mid-session.                                       |
| `!who`                     | Lists all currently connected users.                                     |
| `!commands`                | Displays a list of available commands.                                   |
| `!about`                   | Shows version and author information.                                    |
| `!whisper Bob Hello!`      | Sends a private message to user "Bob".                                   |
| `!whisper "Bob Smith" Hi`  | Sends a private message to users with multi-word usernames.              |
| `!roll` or `!roll 100`     | Rolls a random number between 1–6 (or up to 100 if specified).           |
| `!kick Alex`               | (Moderator only) Kicks the user "Alex" from the server.                  |
| `!exit`                    | Disconnects you from the chat server.                                    |

## ⚠️ Notes

- Only the host is a moderator by default.
- Moderators cannot kick each other or themselves.
- Duplicate usernames are not allowed.
- The server uses non-blocking async sockets to allow smooth UI updates and multiple connections.

## 🔧 Requirements

- .NET Framework or .NET Core (Windows Forms compatible)
- Visual Studio or compatible C# IDE

## 📚 References

- GeeksforGeeks. (2018, September 12). TCP ServerClient implementation in C. GeeksforGeeks. https://www.geeksforgeeks.org/c/tcp-server-client-implementation-in-c/
- Lorenarms. (2023, February 13). Super Simple TCP Server Messenger Application C#. [YouTube]. https://www.youtube.com/watch?v=c3zFzHXvKS8
- Microsoft. (2021, September 15). Separate strings into substrings - .NET. Microsoft.com. https://learn.microsoft.com/en-us/dotnet/standard/base-types/divide-up-strings
- Microsoft. (2022, December 1). Use Sockets to send and receive data over TCP - .NET. Learn.microsoft.com. https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/socket-services#create-a-socket-server
- Microsoft. (2024a). Encoding.UTF8 Property (System.Text). Microsoft.com. https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.utf8?view=net-9.0
- Microsoft. (2024b). Socket Class (System.net.Sockets). Microsoft.com. https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket?view=net-9.0
- Microsoft. (2024c, April 17). Use TcpClient and TcpListener - .NET. Microsoft.com. https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes?source=recommendations
- Microsoft. (2025, May 7). What is Windows Forms - Windows Forms. Microsoft.com. https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/

## 💡 License

This project is for educational use only. Attribution appreciated but not required.
