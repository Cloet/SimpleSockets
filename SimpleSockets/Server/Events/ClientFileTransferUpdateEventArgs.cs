using System;
using System.IO;

namespace SimpleSockets.Server {

    public class ClientFileTransferUpdateEventArgs: EventArgs {

        internal ClientFileTransferUpdateEventArgs(ISessionInfo client, int part, int totalParts, FileInfo file, string dest) {
            ClientInfo = client;
            Part = part;
            TotalParts = totalParts;
            PercentageSent = (( Double.Parse(part.ToString()) / Double.Parse(totalParts.ToString()) ) * 100);
            File = file;
            FileDestination = dest;
        }

		public ISessionInfo ClientInfo { get; protected set; }

        public FileInfo File { get; private set; }

        public string FileDestination { get; private set; }

        public int Part { get; private set; }

        public int TotalParts { get; private set; }

        public double PercentageSent { get; private set; }

    }
    
}