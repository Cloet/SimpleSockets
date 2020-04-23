using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;
using SimpleSockets.Messaging;

namespace SimpleSockets.Server {

    public abstract class SimpleServer: SimpleSocket
    {

        #region Events
        
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        public event EventHandler<ClientDisConnectedEventArgs> ClientDisConnected;

        #endregion

        #region Variables

        private Action<string> _logger;

        public override Action<string> Logger { 
            get => _logger;
            set {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                SocketLogger = LogHelper.InitializeLogger(false, SslEncryption , SocketProtocolType.Tcp == this.SocketProtocol, value, this.LogLevel);
                _logger = value;
            }
        }

        public int MaximumConnections { get; private set; } = 500;

        protected readonly ManualResetEventSlim CanAcceptConnections = new ManualResetEventSlim(false);

        protected Socket Listener { get; set; }

        public bool Listening { get; set; }

        public int ListenerPort { get; set; }

        public string ListenerIp { get; set; }

        internal IDictionary<int, IClientMetadata> ConnectedClients { get; private set; }

        #endregion

        protected SimpleServer() {
            Listening = false;
        }

        public abstract void Listen(string ip, int port, int limit = 500);

        public void Listen(int port , int limit = 500) => Listen("*", port, limit);

        protected abstract void OnClientConnect(IAsyncResult result);

        protected IPAddress ListenerIPFromString(string ip) {
            try {

                if (string.IsNullOrEmpty(ip) || ip.Trim() == "*"){
                    var ipAdr = IPAddress.Any;
                    ListenerIp = ipAdr.ToString();
                    return ipAdr;
                }

                return IPAddress.Parse(ip);
            } catch (Exception ex ){
                SocketLogger?.Log("Invalid Server IP",ex,LogLevel.Fatal);
                return null;
            }
        }

        public bool IsClientConnected(int id) {
            try {
                if (RetrieveClientMetadataById(id) is IClientMetadata state && state.Listener is Socket socket) {
                    return !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);
                }
            } catch (ObjectDisposedException) {
                return false;
            }
            catch (Exception ex) {
                SocketLogger?.Log("Something went wrong trying to check if a client is conntected.", ex, LogLevel.Error);
            }

            return false;
        }

        public void ShutDownClient(int id) {
            ShutDownClient(id, DisconnectReason.Normal);
        }

        internal void ShutDownClient(int id, DisconnectReason reason) {
            var client = RetrieveClientMetadataById(id);

            if (client == null)
                SocketLogger?.Log("Cannot shutdown client " + id + ", does not exist.", LogLevel.Warning);

            try {
                if (client?.Listener != null) {
                    client.Listener.Shutdown(SocketShutdown.Both);
                    client.Listener.Close();
                    client.Listener = null;
                }
            } catch (ObjectDisposedException de) {
                SocketLogger?.Log("Cannot shutdown client " + id + ", has already been disposed.",de, LogLevel.Warning);
            } catch (Exception ex) {
                SocketLogger?.Log("An error occurred when shutting down client " + id, ex, LogLevel.Warning);
            } finally {
                lock(ConnectedClients) {
                    ConnectedClients.Remove(id);
                    var cd = new ClientDisConnectedEventArgs(client, reason);
                    ClientDisConnected?.Invoke(this, cd);
                }
            }
        }

        internal IClientMetadata RetrieveClientMetadataById(int id) {
            return ConnectedClients.TryGetValue(id, out var state) ? state : null;
        }

        public IClientInfo RetrieveClientInfoById(int id) {
            return RetrieveClientMetadataById(id);
        }

        #region Sending Data

        protected abstract void SendToSocket(int clientId, byte[] payload, bool shutdownClient);

        public bool SendMessage(int clientId, string message, IDictionary<object,object> metadata, bool shutdownClient = false) {

            var payload = FluentMessageBuilder.Initialize(MessageType.Message, SocketLogger)
                .AddCompression(CompressionMethod)
                .AddEncryption(EncryptionPassphrase,  EncryptionPassphrase != null ? EncryptionMethod : EncryptionType.None)
                .AddMessageString(message)
                .AddMetadata(metadata)
                .BuildMessage();

            return true;
        }

        #endregion

    }

}