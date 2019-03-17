# AsyncClientServer
Library implementing Async client and Async Server.
The client and server connect using tcp.
It supports sending objects as xml, sending files and folder, sending messages and sending commands.
I've implemented basic compression and encryption for files. Messages are encrypted using aes256.

## Done
- Send messages from server -> client & server -> client
- Ability to send files from client -> server & server -> client
- Serialize objects into xml and send from client -> server & server -> client
- Compression algorithm (GZipStream and .zip support added)
- Added Cryptography (AES256)
- Can send large files.

## TODO
- Convert objects to json and send these.
- Add More compression
- Need to check reliablity when sending from multiple clients to the server.
- ?

## Usage
### Server

Starts the server
```C#
//Starts the server
int port = 13000;
AsyncSocketListener.Instance.StartListening(port);
```

Bind the events to methods in your program
```C#
//Events
AsyncSocketListener.Instance.ProgressFileReceived += new FileTransferProgressHandler(Progress);
AsyncSocketListener.Instance.MessageReceived += new MessageReceivedHandler(MessageReceived);
AsyncSocketListener.Instance.MessageSubmitted += new MessageSubmittedHandler(MessageSubmitted);
AsyncSocketListener.Instance.ClientDisconnected += new ClientDisconnectedHandler(ClientDisconnected);
AsyncSocketListener.Instance.ClientConnected += new ClientConnectedHandler(ClientConnected);
AsyncSocketListener.Instance.FileReceived += new FileFromClientReceivedHandler(FileReceived);
AsyncSocketListener.Instance.ServerHasStarted += new ServerHasStartedHandler(ServerHasStarted);
```
```C#
//Methods
void MessageReceived(int id, string header, string msg);
void MessageSubmitted(int id, bool close);
void FileReceived(int id, string path);
void Progress(int id, int bytes, int messageSize);
void ServerHasStarted();
void ClientConnected(int id);
void ClientDisconnected(int id);
```

### Client

Starts the client
```C#
//Start the client
AsyncClient client = new AsyncClient();
String ipServer = "127.0.0.1";
int portServer = 13000;
client.Startclient(ipServer,portServer);
```

Binding the events
```C#
//Events
client.ProgressFileReceived += new ProgressFileTransferHandler(Progress);
client.Connected += new ConnectedHandler(ConnectedToServer);
client.MessageReceived += new ClientMessageReceivedHandler(ServerMessageReceived);
client.MessageSubmitted += new ClientMessageSubmittedHandler(ClientMessageSubmitted);
client.FileReceived += new FileFromServerReceivedHandler(FileReceived);
client.Disconnected += new DisconnectedFromServerHandler(Disconnected);
```

```C#
//Events
void ConnectedToServer(IAsyncClient a);
void ServerMessageReceived(IAsyncClient a, string header, string msg);
void FileReceived(IAsyncClient a, string file);
void Disconnected(IAsyncClient a, string ip, int port);
void Progress(IAsyncClient a, int bytes, int messageSize);
void ClientMessageSubmitted(IAsyncClient a, bool close);
```
