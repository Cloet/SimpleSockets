# SimpleSockets
[![NuGet version](https://badge.fury.io/nu/SimpleSockets.svg)](https://badge.fury.io/nu/SimpleSockets)

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
I've added several classes that can be used to augment how the messaging of your sockets work.
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

| Server | Description |
|--------| ------------|
| SimpleSocketListener    | The base Listener class. |
| SimpleSocketTcpListener    | Server using Async calls without using Ssl. |
| SimpleSocketTcpSslListener | Server using Async calls that uses a Ssl certificate to encrypt incoming & outgoing data.| 

[Wiki](https://github.com/Cloet/SimpleSockets/wiki/Server)

### Client

There are two different options of async client:

| Client | Description|
|--------| -----------|
| SimpleSocketClient       | Base SimpleSocketClient class |
| SimpleSocketTcpClient    | Client using Async Calls without using Ssl. |
| SimpleSocketTcpSslClient | An async client that uses an ssl certificate to encrypt incoming & outgoing data. By default the client does not need to authenticate. Only the server requires to be authenticated, the constructor has the option to require server and client authentication.| 

[Wiki](https://github.com/Cloet/SimpleSockets/wiki/Client)


