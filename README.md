# SimpleSockets
[![NuGet version](https://badge.fury.io/nu/SimpleSockets.svg)](https://badge.fury.io/nu/SimpleSockets)

SimpleSockets is a .NET Standard 2.1 library with integrated framing and ssl support that provides an easy way to build fast and easy to use Tcp/Ip programs.


## Table of Contents

- [Info](#Info)
- [Install](#install)
- [Usage](#usage)
    - [Server](#server)
    - [Client](#client)
- [Customization](#customization)
    - [Encryption] (#encryption)
    - [Compression] (#compression)
- [Maintainers](#maintainers)
- [Contributing](#contributing)
- [License](#license)

## Framing
Because Tcp/ip just sends a stream to a connected socket, it's impossible to know where a message starts and another one stops. To aid with easier messaging between sockets this library uses message framing. [Wiki](https://github.com/Cloet/SimpleSockets/wiki/Framing)


## Install

This project uses [dotnetcore 2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1).  
The latest version of the package can always be found on [nuget](https://www.nuget.org/packages/SimpleSockets/).
```sh
PM> Install-Package SimpleSockets
```
or using .NET CLI
```sh
> dotnet add package SimpleSockets
```

## Usage

### Server

There are two different options of SimpleSocketListener

| Server | Description |
|--------| ------------|
| SimpleSocketListener    | The base Listener class. |
| SimpleSocketTcpListener    | Server using Async calls without using Ssl. |
| SimpleSocketTcpSslListener | Server using Async calls that uses a Ssl certificate to encrypt incoming & outgoing data.| 

**To help authenticate connected clients on the server side, whenever a client connects to the server it will send an BasicAuth message -> this contains a GUID and the OSVersion the client is running. When the client sets 'EnableExtendedAuth' to true it will send a GUID, OSVersion, Pc Name and DomainName.**

[Wiki](https://github.com/Cloet/SimpleSockets/wiki/Server)

### Client

There are two different options of async client:

| Client | Description|
|--------| -----------|
| SimpleSocketClient       | Base SimpleSocketClient class |
| SimpleSocketTcpClient    | Client using Async Calls without using Ssl. |
| SimpleSocketTcpSslClient | An async client that uses an ssl certificate to encrypt incoming & outgoing data. By default the client does not need to authenticate. Only the server requires to be authenticated, the constructor has the option to require server and client authentication.| 

[Wiki](https://github.com/Cloet/SimpleSockets/wiki/Client)

## Customization
I've added several classes that can be used to augment how the messaging of your sockets work.
### Encryption
If you don't want to use Ssl, but don't want to send plaintext over the internet you can use the default Encryption added to the Library, which uses AES, the salt and key can be changed but have to correspond between client and server.  
**!! Warning **
This does not make your connection safe!
If you're concerned with safety use Ssl !!**  

**Custom Encryption**  
*You can implement your own custom Encryption for client and server. Implement the interface IMessageEncryption and change the MessageEncryption of the client and server, if connected sockets are not using the same encryption it won't work.*

### Compression
By default folders will be compressed using ZIP compression before they are sent.
All other types of messages won't be compressed. There are default classes for compressing, files, folders and Stream. Compression isn't always a good idea to use, if you're sending a small message there is a chance your message will actually increase in size when sending.
(ByteCompressor is for all messages except files, folder). 

**Custom Compression**  
*For folders: implement IFolderCompression and change the FolderCompressor of client, server.*  
*For Files  : implement IFileCompression and change the FileCompressor property of client, server.*  
*For Stream : implement IByteCompression and change the ByteCompressor property of client, server.*  


## Maintainers

[@Cloet](https://github.com/Cloet).

## Contributing

Feel free to dive in! [Open an issue](https://github.com/Cloet/SimpleSockets/issues) or submit PRs.

### Contributors

This project exists thanks to all the people who contribute. 



## License

[MIT](LICENSE) © Mathias Cloet
