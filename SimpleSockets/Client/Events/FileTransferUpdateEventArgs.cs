using System;
using System.IO;

namespace SimpleSockets.Client {

    public class FileTransferUpdateEventArgs: EventArgs {

        internal FileTransferUpdateEventArgs(long part, long totalParts, FileInfo file, string dest) {
            Part = part;
            TotalParts = totalParts;
            Percentage = ((double) part / totalParts );
            File = file;
            FileDestination = dest;

            if (Percentage >= 1 && part < totalParts)
                Percentage = 0.99;
        }

        public FileInfo File { get; private set; }

        public string FileDestination { get; private set; }

        public long Part { get; private set; }

        public long TotalParts { get; private set; }

        public double Percentage { get; private set; }

    }
    
}