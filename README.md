# AsyncClientServer with ssl implementation
Library implementing Async client and Async Server.
The client and server connect using tcp.

## SSL
### Creating a certificate
There is a powershell script include (Self-SignedCertificate Script.ps1) if you want to create an SSL certificate for testing purposes.
The default password will be "Password" and the certificate will be saved on the desktop.
### SSL Protocols supported
The constructor for AsyncSocketSslListener and AsyncSslClient both have an option to specify which tls version the client and/or server should use. They are compatible with Tls1.0, Tls1.1 and Tls1.2 (Tls1.2 by default), client and server should use the same version.

## Server
There are two different options of async server:
 - AsyncSocketListener    -> A regular async server without ssl
 - AsyncSocketSslListener -> An async server that uses an ssl certificate to encrypt incoming & outgoing data.
 
## Client
There are two different options of async client:
 - AsyncClient            -> A regular async client wihtout ssl
 - AsyncSslClient         -> An async client that uses an ssl certificate to encrypt incoming & outgoing data.
                             By default the client does not need to authenticate. Only the server requires to be authenticated
                             The constructor has the option to disable this.
                             
## Compression
Files will be compressed before being sent to either client or server by default. (This can be turned off)
Folder will be compressed to '.Zip' before being sent to either client or server. (This cannot be turned off)
Files   use .Gcz Compression
Folders use .Zip Compression

## Encryption
If you don't want to use SSL but still want to be a little safer. Or if you just don't want to send plaintext when using a regular Client Server, there is an option to encrypt files and data with AES256.
### Warning
THIS DOES NOT MAKE YOUR CONNECTION SAFE.
If you're really concered with safety use the SSL variant of the client and server.
### Requirements
The Client and server will need the same key to decrypt and encrypt data, files, else it will return an error on receipt.
For now there is no way of setting the key, it is hardcoded.


## Framing
### How messages are framed
The client or server sends a byte array, this array consists of:
1. HeaderLength    [4 First bytes of the array]
2. MessageLength   [4-8 bytes of the array are reserverd]
3. Header          [8+Headerlength of the array]
4. MessageData     [8+HeaderLength+MessageLength of the array]
### Why
This way if multiple messages are received the client or server can correctly handle all of them and according to the header distinguish what type of message is received (Message,Command or file/folder) 

## Usage
### Server

Creating the Server
```C#
//Create Regular server
IServerlistener listener = new AsyncSocketListener();
//Create Ssl server
//(Certificate will be a .pfx) file
IServerlistener listener = new AsyncSocketSslListener("path to certificate","password");
```

Starts the server
```C#
//Starts the server
string ip = "127.0.0.1"
int port = 13000;
listener.StartListening(ip,port);
```

Bind the events to methods in your program
```C#
//Events
listener.ProgressFileReceived += new FileTransferProgressHandler(Progress);
listener.MessageReceived += new MessageReceivedHandler(MessageReceived);
listener.MessageSubmitted += new MessageSubmittedHandler(MessageSubmitted);
listener.ClientDisconnected += new ClientDisconnectedHandler(ClientDisconnected);
listener.ClientConnected += new ClientConnectedHandler(ClientConnected);
listener.FileReceived += new FileFromClientReceivedHandler(FileReceived);
listener.ServerHasStarted += new ServerHasStartedHandler(ServerHasStarted);
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

Creating a client
```C#
//Regular async client
ITcpClient client = new AsyncClient();
//Ssl async client
//(Certificate will be a .pfx) file
ITcpClient client = new AsyncSslClient("Path to Certificate","Password");
```

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
