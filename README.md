# TCP Chat + Tic-Tac-Toe Game  

A multiclient TCP chat server written in C# with an integrated Tic-Tac-Toe game. This project demonstrates asynchronous socket-based networking, database persistence for player scores, and a synchronized multiplayer game experience — all within a Windows Forms UI.  

## 🚀 Features  

- **Multiplayer Chat System**  
  - Host or join a TCP chat server on a specified port.  
  - Multiple clients with unique usernames and passwords.  
  - Private messaging with `!whisper`.  
  - Moderator system with `!kick` functionality.  
  - Rich command list for both chat and game control.  

- **Tic-Tac-Toe Game Integration**  
  - Play Tic-Tac-Toe directly in the client form.  
  - Board updates synchronized in real-time between players.  
  - Game state stored in a persistent SQLite database.  
  - Automatic win/loss/draw tracking.  
  - Colour-coded board state (violet = active game, grey = inactive).  

- **Score Tracking**  
  - `!scores` command displays all players’ wins, losses, and draws.  
  - Scores are sorted from highest wins to lowest.  

## 🛠️ How to Run

1. Open the solution in Visual Studio.
2. Build and run the project.
3. Optionally open at "A2_C#_Chat_App\Windows Forms core chat\bin\Debug\netcoreapp3.1\Windows Forms core chat.exe".

### Server

1. Enter the desired port number in the "My Port" field (filled out by default).
2. Press the “Host Server” button to begin hosting on that desired port.
3. Once connected, begin sending commands/messages.

Server can:

- Send global chat messages.  
- Manage moderators.  
- Kick players.
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

- Send `!login YourUsername YourPassword` or `!register YourUsername YourPassword` upon connection.
- Enter lobby and chat using supported commands to interact with others,
- Or, `!join` a game to enter a game session.

### 💬 Client Commands

| Command                    | Description                                                              |
|----------------------------|--------------------------------------------------------------------------|
| `!user Alex2`              | Changes your username mid-session (must be unique).                      |
| `!who`                     | Lists all connected users.                                               |
| `!commands`                | Lists available commands.                                                |
| `!about`                   | Shows version and author info.                                           |
| `!join`                    | Register to join a Tic-Tac-Toe session.                                  |
| `!scores`                  | Shows all player scores sorted by wins (highest first).                  |
| `!whisper Bob Hello!`      | Sends a private message to "Bob".                                        |
| `!roll` or `!roll 100`     | Rolls a random number between 1–6 (or 1–100).                            |
| `!kick Alex`               | (Moderator only) Kicks "Alex".                                           |
| `!exit`                    | Disconnects from the server.                                             |

---

## 🎮 Game Commands  

| Command                      | Description                                                              |
|------------------------------|--------------------------------------------------------------------------|
| `!startgame`                 | Starts a Tic-Tac-Toe match with another player.                          |
| `!move Index X/O`            | Sets a tile (0–8 index) as X or O during your turn.                      |
| `!exit`                      | Closes session and disconnects from the server.                          |

---                            

## ⚠️ Notes

- Only the host is a moderator by default.
- Moderators cannot kick each other or themselves.
- Duplicate usernames are not allowed.

---

## ⚙️ Technical Details  

- **Networking:** Asynchronous sockets via `System.Net.Sockets` for smooth UI updates.  
- **Database:** SQLite integration for score persistence.  
- **UI:** Windows Forms with live board updates.  
- **Architecture:** Modular design with `TCPChatServer`, `TCPChatClient`, `TCPChatBase`, `GameStateManager`, `DatabaseManager`, and `TicTacToe`.  

---

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
- Somani, S. (2022). Tic-Tac-Toe Game in C#. Www.c-Sharpcorner.com. https://www.c-sharpcorner.com/UploadFile/75a48f/tic-tac-toe-game-in-C-Sharp/
- SQLite. (2019). SQLite Home Page. Sqlite.org. https://sqlite.org/

## 💡 License

This project is for educational use only. Attribution appreciated but not required.
