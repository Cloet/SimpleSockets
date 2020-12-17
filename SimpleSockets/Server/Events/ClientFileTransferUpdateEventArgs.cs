using System;
using System.IO;

namespace SimpleSockets.Server {

    public class ClientFileTransferUpdateEventArgs: EventArgs {

        internal ClientFileTransferUpdateEventArgs(ISessionInfo client, long part, long totalParts, FileInfo file, string dest) {
            ClientInfo = client;
            Part = part;
            TotalParts = totalParts;
            Percentage = ((double) part / totalParts );
            File = file;
            FileDestination = dest;

            if (Percentage >= 1 && part < totalParts)
                Percentage = 0.99;
        }

		public ISessionInfo ClientInfo { get; protected set; }

        public FileInfo File { get; private set; }

        public string FileDestination { get; private set; }

        public long Part { get; private set; }

        public long TotalParts { get; private set; }

        public double Percentage { get; private set; }

    }
    
}