# AsyncClientServer with ssl implementation

[![NuGet version](https://badge.fury.io/nu/AsyncClientServer.svg)](https://badge.fury.io/nu/AsyncClientServer)

.NET Library using sockets to connect client to server using Asynchronous programming.
The library is written in .NET Standard 2.0

## SSL
### Creating a certificate
I've included a powershell script (Self-SignedCertificate Script.ps1), you can use this script to create a Ssl certificate for testing purposes.  
The default password of the certificate will be "Password" and the certificate will be saved on the desktop.  
### SSL Protocols supported
The AsyncSocketSslListener and AsyncSslClient both support Tls1.0, Tls1.1 and Tls1.2 (Tls1.2 is the default).  
You can specify the Tls version in the constructor if none entered Tls1.2 will be used.  
The client and server have to use the same version.                            
                                                         
## Compression
By Default files and folder will be compressed before they are sent.
Files will be compressed before being sent. (This can be turned off)  
Folder will be compressed before being sent. (This cannot be turned off)  
Files   use .Gcz Compression  
Folders use .Zip Compression  
### Using your own compression
You can change the FileCompression by changing the "FileCompressor" property of the client and server with a class that has been extended with the FileCompressor class.  
You can change the FolderCompression by change the "FolderCompressor" property of the client and server with a class that has been extended with FolderCompressor class.  

## Encryption
If you don't want to use Ssl but don't want to send plaintext over the internet, there is an option to encrypt files and data with AES256.  
You can change the salt and key the client and server use.  
### Warning
THIS DOES NOT MAKE YOUR CONNECTION SAFE.  
If you're really concered with safety use the Ssl variant of the client and server.
### Requirements
The Client and Server will need the same key and salt to encrypt and decrypt data and files. If they do not they will return an error on receipt of data.  
To change the salt or key use 'ChangeSalt()' or 'ChangeKey()' method in Client or Server.  
### Using your own encryption
You can use your own encryption and decryption by creating a new class that extends "Encryption" in AsyncClientServer and then changing the MessageEncrypter of the server and client.

## File Transfer
By default the client and/or server does not allow receiving files => AllowReceivingFiles = False by default.
If you want your socket to receive files set this to true.

## Framing
### How the message are framed.
The client or server sends a byte array, this array consists of:

| Message part | Location in byte array | More info |  
|--------------|------------------------|-----------|
| HeaderLength | 4 first bytes of the array | The length of the header part of the byte array. |
| MessageLength | 4-8 bytes of the array    | The length of the message part of the byte array. |
| Header        | 8 + HeaderLength of the array | The header of the byte array. |
| MessageData   | 8 + HeaderLength + MessageLength of the array | The part where the message is contained. |

### Reason for using framing
Using framed message makes sure multiples message can be correctly handles by the server or client and have the ability to distinguish what type of message is received.

## MessageContracts
This way you can create your own MessageTypes.
You have to implement the interface IMessageContract and add the contract to both your server and client.
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


## Usage
### Server

There are two different options of async client:

| Server | Description|
|--------| -----------|
| AsyncSocketListener    | Server using Async calls without using Ssl. |
| AsyncSocketSslListener | Server using Async calls that uses a Ssl certificate to encrypt incoming & outgoing data.| 

Creating the Server
```C#
//Create Regular server
Serverlistener listener = new AsyncSocketListener();
//Create Ssl server
//(Certificate will be a .pfx) file
Serverlistener listener = new AsyncSocketSslListener("path to certificate","password");
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
listener.MessageFailed += new DataTransferToClientFailedHandler(MessageFailed);
listener.CustomHeaderReceived += new CustomHeaderMessageReceivedHandler(CustomHeaderReceived);
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
void MessageFailed(int id, byte[] messageData, string exceptionMessage);
void CustomHeaderReceived(int id, string msg, string header);
```

Methods used to send messages to clients
```C#
//Send
public void SendMessage(int id, string message, bool encryptMessage, bool close);
public void SendMessage(int id, string message, bool close);
public async Task SendMessageAsync(int id, string message, bool encryptMessage, bool close);
public async Task SendMessageAsync(int id, string message, bool close);

public void SendFile(int id, string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile, bool close);
public void SendFile(int id, string fileLocation, string remoteSaveLocation, bool close);
public async Task SendFileAsync(int id, string fileLocation, string remoteFileLocation, bool encryptFile, bool compressFile, bool close);
public async Task SendFileAsync(int id, string fileLocation, string remoteFileLocation, bool close);

public void SendFolder(int id, string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close);
public void SendFolder(int id, string folderLocation, string remoteFolderLocation, bool close);
public async Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close);
public async Task SendFolderAsync(int id, string folderLocation, string remoteFolderLocation, bool close);

public void SendCustomHeaderMessage(int id, string message, string header, bool close);
public void SendCustomHeaderMessage(int id, string message, string header, bool encrypt, bool close);
public async Task SendCustomHeaderMessageAsync(int id, string message, string header, bool close);
public async Task SendCustomHeaderMessageAsync(int id, string message, string header, bool encrypt, bool close);

public void SendMessageContract(int id, IMessageContract contract, bool encryptContract, bool close);
public void SendMessageContract(int id, IMessageContract contract, bool close);
public async Task SendMessageContractAsync(int id, IMessageContract contract, bool encryptContract, bool close);
public async Task SendMessageContractAsync(int id, IMessageContract contract, bool close);
```


### Client

There are two different options of async client:

| Client| Description|
|-------| -----------|
| AsyncClient    | Client using Async Calls without using Ssl. |
| AsyncSslClient | An async client that uses an ssl certificate to encrypt incoming & outgoing data. By default the client does not need to authenticate. Only the server requires to be authenticated, the constructor has the option to require server and client authentication.| 

Creating a client
```C#
//Regular async client
SocketClient client = new AsyncClient();
//Ssl async client
//(Certificate will be a .pfx) file
SocketClient client = new AsyncSslClient("Path to Certificate","Password");
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
client.MessageFailed += new DataTransferFailedHandler(MessageFailed);
```

```C#
//Events
void ConnectedToServer(ITcpClient tcpClient);
void ServerMessageReceived(ITcpClient tcpClient, string header, string msg);
void FileReceived(ITcpClient tcpClient, string file);
void Disconnected(ITcpClient tcpClient, string ip, int port);
void Progress(ITcpClient tcpClient, int bytes, int messageSize);
void ClientMessageSubmitted(ITcpClient tcpClient, bool close);
void MessageFailed(ITcpClient tcpClient, byte[] messageData, string exceptionMessage)
```

Methods used to send messages to server
```C#
//Send to server
public void SendMessage(string message, bool encryptMessage, bool close);
public void SendMessage(string message, bool close);
public async Task SendMessageAsync(string message, bool encryptMessage, bool close);
public async Task SendMessageAsync(string message, bool close);

public void SendFile(string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile, bool close);
public void SendFile(string fileLocation, string remoteSaveLocation, bool close);
public async Task SendFileAsync(string fileLocation, string remoteFileLocation, bool encryptFile, bool compressFile, bool close);
public async Task SendFileAsync(string fileLocation, string remoteFileLocation, bool close);

public void SendFolder(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close);
public void SendFolder(string folderLocation, string remoteFolderLocation, bool close);
public async Task SendFolderAsync(string folderLocation, string remoteFolderLocation, bool encryptFolder, bool close);
public async Task SendFolderAsync(string folderLocation, string remoteFolderLocation, bool close);

public void SendCustomHeaderMessage(string message, string header, bool close);
public void SendCustomHeaderMessage(string message, string header, bool encrypt, bool close);
public async Task SendCustomHeaderMessageAsync(string message, string header, bool close);
public async Task SendCustomHeaderMessageAsync(string message, string header, bool encrypt, bool close);

public void SendMessageContract(IMessageContract contract, bool encryptContract, bool close);
public void SendMessageContract(IMessageContract contract, bool close);
public async Task SendMessageContractAsync(IMessageContract contract, bool encryptContract, bool close);
public async Task SendMessageContractAsync(IMessageContract contract, bool close);
```

Broadcast messages for server
```C#
public void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool encryptFile,bool compressFile, bool close);
public void SendFileToAllClients(string fileLocation, string remoteSaveLocation, bool close);
public async Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool encryptFile, bool compressFile,bool close);
public async Task SendFileToAllClientsAsync(string fileLocation, string remoteSaveLocation, bool close);

public void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool encryptFolder,bool close);
public void SendFolderToAllClients(string folderLocation, string remoteFolderLocation, bool close);
public async Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool encryptFolder,bool close);
public async Task SendFolderToAllClientsAsync(string folderLocation, string remoteFolderLocation, bool close);

public void SendMessageToAllClients(string message, bool encryptMessage, bool close);
public void SendMessageToAllClients(string message, bool close);
public async Task SendMessageToAllClientsAsync(string message, bool encryptMessage, bool close);
public async Task SendMessageToAllClientsAsync(string message, bool close);

public void SendCustomHeaderToAllClients(string message, string header, bool encryptMessage, bool close);
public void SendCustomHeaderToAllClients(string message, string header, bool close);
public async Task SendCustomHeaderToAllClientsAsync(string message, string header, bool encryptMessage,bool close);
public async Task SendCustomHeaderToAllClientsAsync(string message, string header, bool close);

```
## Class Diagram of AsyncSockets
<img src='https://i.imgur.com/GqoAZI3.png' />

