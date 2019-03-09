# AsyncClientServer
Simple library using Asynchronous client and Asynchronous server and connecting using tcp.

The server and client can send & receive: files, objects and messages.

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
- Better support for sending files.
- Make filetransfer for big files faster.
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
AsyncSocketListener.Instance.FileReceived += new FileFromClientReceivedHandler(FileReceived);
AsyncSocketListener.Instance.ServerHasStarted += new ServerHasStartedHandler(ServerHasStarted);
```
```C#
//Methods
private static void MessageReceived(int id, string header,string msg)
{
	//Code
}

private static void MessageSubmitted(int id, bool close)
{	
	//Code
}

private static void FileReceived(int id, string path)
{
	//Code
}

private static void Progress(int id, int bytes, int messageSize)
{
	//Code
}

private static void ServerHasStarted()
{
	//Code
}

private static void ClientDisconnected(int id)
{
	//Code
}
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
private static void ConnectedToServer(IAsyncClient a)
{
	//Code
}

private static void ServerMessageReceived(IAsyncClient a,string header, String msg)
{
	//Code
}

private static void FileReceived(string file)
{
	//Code
}

private static void Disconnected(string ip, int port)
{
	//Code
}

private static void Progress(IAsyncClient a, int bytes, int messageSize)
{
	//Code
}

private static void ClientMessageSubmitted(IAsyncClient a, bool close)
{
	//Code
}
```
