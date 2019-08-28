# SimpleSockets
[![NuGet version](https://badge.fury.io/nu/AsyncClientServer.svg)](https://badge.fury.io/nu/AsyncClientServer)

.NET Library implementing Asynchronous usage of Sockets. The Library is written in .NET Standard 2.0 and supports Ssl.

Changed From AsyncClientServer to SimpleSockets

## SSL
### Creating a certificate
I've included a powershell scrit (Self-SignedCertificate Script.ps1). I've used this script to create an certificate to test the Ssl implementation.
### Protocols
The SimpleSocketTcpSslClient and SimpleSocketTcpSslListener both support Tls1.0, Tls1.1 and Tls1.2 (TLS1.2 is the default.). Just make sure the client and server match Ssl Protocol.

## Framing
Because Tcp just sends a stream to the connected socket, it's impossible to know where 1 message starts and another one stops, for this reason this library uses message framing.
The first byte contains Flags, these flags determine what parts are present in the message.

Every type of message can have a different type of framing.
Below is the max amount that can be used when framing a message.

|Message part   | Location in byte array | More info |
|---------------|------------------------|-----------|
| Flags         | 0     - 1 byte           | Contains 8 possible boolean values |
| MessageLength | 1-4   - 4 bytes          | The total amount of bytes the message contains |
| MessageType   | 5     - 1 byte           | 255 Possible types of different messages |
| HeaderLength  | 6-10  - 4 bytes          | The length of the header |
| FilePart      | 10-14 - 4 bytes          | The current part of the filetransfer |
| TotalParts    | 14-18 - 4 bytes          | Total amount of parts of the filetransfer |
| HeaderData    | 18 + HeaderLength        | The Header bytes |
| MessageData   | 18 + HeaderLength + MessageLength | Message bytes |

When framing a normal message this will be a lot smaller:

|Message part   | Location in byte array | More info |
|---------------|------------------------|-----------|
| Flags         | 0     - 1 byte           | Contains 8 possible boolean values |
| MessageLength | 1-4   - 4 bytes          | The total amount of bytes the message contains |
| MessageType   | 5     - 1 byte           | 255 Possible types of different messages |
| MessageData   | 18 + HeaderLength + MessageLength | Message bytes |

## Customization
I've added several classes that can be used to augment how the messageing of your sockets work.
### MessageContracts
With MessageContracts you can send custom type of message to a connected socket.
You have to implement the interface IMessageContract and add both the contract to the server and client.
```C
//**Server
//Create a new MessageContract, MessageA and add it to the server.
//Bind OnmessageReceived of the Contract to receive the message sent from the clients.
_messageAContract = new MessageA("MessageAHeader");
_listener.AddMessageContract(_messageAContract);
_messageAContract.OnMessageReceived += MessageAContractOnOnMessageReceived;
//**Client
//Create the MessageContract implementation and add to the client
_messageAContract = new MessageA("MessageAHeader");
_client.AddMessageContract(_messageAContract);
_messageAContract.OnMessageReceived += MessageAContractOnOnMessageReceived;
```
### Encryption
If you don't want to use Ssl, but don't want to send plaintext over the internet you can use the default Encryption added to the Library, which uses AES, the salt and key can be changed but have to correspond between client and server.
#### Warning
This does not make your connection safe!
If you're concerned with safety use Ssl.
#### Custom Encryption
You can implement your own custom Encryption for client and server. Implement the interface IMessageEncryption and change the MessageEncryption of the client and server, if connected sockets are not using the same encryption it won't work.
### Compression
By default folders will be compressed using ZIP compression before they are sent.
All other types of messages won't be compressed. There are default classes for compressing, files, folders and Stream. Compression isn't always a good idea to use, if you're sending a small message there is a chance your message will actually increase in size when sending.
(ByteCompressor is for all messages except files, folder).
#### Custom Compression
For folders: implement IFolderCompression and change the FolderCompressor of client, server.
For Files  : implement IFileCompression and change the FileCompressor property of client, server.
For Stream : implement IByteCompression and change the ByteCompressor property of client, server.

## File Transfer
By default the socket will not accept Files from a connected socket. Set AllowReceivingFiles to true to accept incoming files.

## Client Authentication
To help authenticate connected clients on the server side, whenever a client connects to the server it will send an BasicAuth message -> this contains a GUID and the OSVersion the client is running. When the client sets 'EnableExtendedAuth' to true it will send a GUID, OSVersion, Pc Name and DomainName.

## Usage
### Server

There are two different options of SimpleSocketListener
| Server | Description|
|--------| -----------|
| SimpleSocketListener    | The base Listener class. |
| SimpleSocketTcpListener    | Server using Async calls without using Ssl. |
| SimpleSocketTcpSslListener | Server using Async calls that uses a Ssl certificate to encrypt incoming & outgoing data.| 


Creating the Server
```C#
//Create Regular server
SimpleSocketListener listener = new SimpleSocketTcpListener();
//Create Ssl server
//(Certificate will be a .pfx) file
SimpleSocketListener listener = new SimpleSocketTcpSslListener("path to certificate","password");
```

Starts the server
```C#
//Starts the server
string ip = "127.0.0.1"
int port = 13000;
listener.StartListening(ip,port);

Events
The server has various events that have to be bound.
Below is an example of how I bind my events.
    
```C#
//Events
_listener.AuthFailure += ListenerOnAuthFailure;
_listener.AuthSuccess += ListenerOnAuthSuccess;
_listener.FileReceiver += ListenerOnFileReceiver;
_listener.FolderReceiver += ListenerOnFolderReceiver;
_listener.MessageReceived += MessageReceived;
_listener.MessageSubmitted += MessageSubmitted;
_listener.CustomHeaderReceived += CustomHeaderReceived;
_listener.ClientDisconnected += ClientDisconnected;
_listener.ClientConnected += ClientConnected;
_listener.ServerHasStarted += ServerHasStarted;
_listener.MessageFailed += MessageFailed;
_listener.ServerErrorThrown += ErrorThrown;
_listener.ObjectReceived += ListenerOnObjectReceived;
_listener.MessageUpdateFileTransfer += ListenerOnMessageUpdateFileTransfer;
_listener.MessageUpdate += ListenerOnMessageUpdate;
```
Methods used to send messages to clients
```C#
//Send
public void SendMessage(int id,string message, bool compress = false, bool encrypt = false, bool close = false);
public async Task SendMessageAsync(int id,string message, bool compress = false, bool encrypt = false, bool close = false);

public void SendBytes(int id,byte[] data, bool compress = false, bool encrypt = false, bool close = false);
public async Task SendBytesAsync(int id,byte[] data, bool compress = false, bool encrypt = false, bool close = false);

public void SendMessageContract(int id,IMessageContract contract, bool compress = false, bool encrypt = false,bool close = false);
public async Task SendMessageContractAsync(int id,IMessageContract contract, bool compress = false, bool encrypt = false, bool close = false);

public void SendCustomHeader(int id,string message, string header, bool compress = false, bool encrypt = false, bool close = false);
public void SendCustomHeader(int id,byte[] data, byte[] header, bool compress = false, bool encrypt = false, bool close = false);
public async Task SendCustomHeaderAsync(int id,string message, string header, bool compress = false, bool encrypt = false, bool close = false);
public async Task SendCustomHeaderAsync(int id,byte[] data, byte[] header, bool compress = false, bool encrypt = false, bool close = false);

public async Task SendFileAsync(int id,string fileLocation, string remoteSaveLocation, bool compress = true, bool encrypt = false, bool close = false);
public void SendFile(int id,string fileLocation, string remoteSaveLocation, bool compress = true, bool encrypt = false, bool close = false);

public async Task SendFolderAsync(int id,string folderLocation, string remoteSaveLocation,bool encrypt = false, bool close = false);
public void SendFolder(int id,string folderLocation, string remoteSaveLocation,bool encrypt = false, bool close = false);

public async Task SendObjectAsync(int id,object obj, bool compress = false, bool encrypt = false, bool close = false);
public void SendObject(int id,object obj, bool compress = false, bool encrypt = false, bool close = false);

```

### Client

There are two different options of async client:

| Client | Description|
|--------| -----------|
| SimpleSocketClient       | Base SimpleSocketClient class |
| SimpleSocketTcpClient    | Client using Async Calls without using Ssl. |
| SimpleSocketTcpSslClient | An async client that uses an ssl certificate to encrypt incoming & outgoing data. By default the client does not need to authenticate. Only the server requires to be authenticated, the constructor has the option to require server and client authentication.| 

Creating a client
```C#
//Regular async client
SimpleSocketClient client = new SimpleSocketTcpClient();
//Ssl async client
//(Certificate will be a .pfx) file
SimpleSocketClient client = new SimpleSocketTcpSslClient("Path to Certificate","Password");
```

Starts the client
```C#
//Start the client
SimpleSocketClient client = new SimpleSocketTcpClient();
String ipServer = "127.0.0.1";
int portServer = 13000;
client.Startclient(ipServer,portServer);
```

Binding the events
The events have to be bound just once per client.
```C#
//Events
_client.AuthSuccess += ClientOnAuthSuccess;
_client.AuthFailed += ClientOnAuthFailed;
_client.FileReceiver += ClientOnFileReceiver;
_client.FolderReceiver += ClientOnFolderReceiver;
_client.DisconnectedFromServer += Disconnected;
_client.MessageUpdateFileTransfer += ClientOnMessageUpdateFileTransfer;
_client.MessageUpdate += ClientOnMessageUpdate;
_client.ConnectedToServer += ConnectedToServer;
_client.ClientErrorThrown += ErrorThrown;
_client.MessageReceived += ServerMessageReceived;
_client.MessageSubmitted += ClientMessageSubmitted;
_client.MessageFailed += MessageFailed;
_client.CustomHeaderReceived += CustomHeader;
_client.ObjectReceived += ClientOnObjectReceived;
```

Methods used to send messages to server
```C#
//Send to server
public void SendMessage(string message, bool compress = false, bool encrypt = false, bool close = false);
public async Task SendMessageAsync(string message, bool compress = false, bool encrypt = false, bool close = false);

public void SendBytes(byte[] data, bool compress = false, bool encrypt = false, bool close = false);
public async Task SendBytesAsync(byte[] data, bool compress = false, bool encrypt = false, bool close = false);

public void SendMessageContract(IMessageContract contract, bool compress = false, bool encrypt = false,bool close = false);
public async Task SendMessageContractAsync(IMessageContract contract, bool compress = false, bool encrypt = false, bool close = false);

public void SendCustomHeader(string message, string header, bool compress = false, bool encrypt = false, bool close = false);
public void SendCustomHeader(byte[] data, byte[] header, bool compress = false, bool encrypt = false, bool close = false);
public async Task SendCustomHeaderAsync(string message, string header, bool compress = false, bool encrypt = false, bool close = false);
public async Task SendCustomHeaderAsync(byte[] data, byte[] header, bool compress = false, bool encrypt = false, bool close = false);

public async Task SendFileAsync(string fileLocation, string remoteSaveLocation, bool compress = true, bool encrypt = false, bool close = false);
public void SendFile(string fileLocation, string remoteSaveLocation, bool compress = true, bool encrypt = false, bool close = false);

public async Task SendFolderAsync(string folderLocation, string remoteSaveLocation,bool encrypt = false, bool close = false);
public void SendFolder(string folderLocation, string remoteSaveLocation,bool encrypt = false, bool close = false);

public async Task SendObjectAsync(object obj, bool compress = false, bool encrypt = false, bool close = false);
public void SendObject(object obj, bool compress = false, bool encrypt = false, bool close = false);

```
