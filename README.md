# AsyncClientServer
Simple library using Asynchronous client and Asynchronous server.

The server and client can send & receive: files, objects and messages.

## Done
- Send messages from server -> client & server -> client
- Ability to send files from client -> server & server -> client
- Serialize objects into xml and send from client -> server & server -> client
- Get a list of all connected clients
- Make it simpler to use
- Add example
- Clean up code

## TODO
- Add more exceptions
- Add file transfer library into it
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
AsyncSocketListener.Instance.MessageReceived += new MessageReceivedHandler(MessageReceived);
AsyncSocketListener.Instance.MessageSubmitted += new MessageSubmittedHandler(MessageSubmitted);
AsyncSocketListener.Instance.ClientDisconnected += new ClientDisconnectedHandler(ClientDisconnected);
AsyncSocketListener.Instance.FileReceived += new FileFromClientReceivedHandler(FileReceived);
AsyncSocketListener.Instance.ServerHasStarted += new ServerHasStartedHandler(ServerHasStarted);
```
```C#
//Methods
private static void MessageReceived(int id,string header, string msg)
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
client.Connected += new ConnectedHandler(ConnectedToServer);
client.MessageReceived += new ClientMessageReceivedHandler(ServerMessageReceived);
client.MessageSubmitted += new ClientMessageSubmittedHandler(ClientMessageSubmitted);
client.FileReceived += new FileFromServerReceivedHandler(FileReceived);
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

private static void ClientMessageSubmitted(IAsyncClient a, bool close)
{
	//Code
}
```
