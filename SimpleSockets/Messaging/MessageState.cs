namespace SimpleSockets.Messaging {

    public enum MessageState {

        ReceivingHeader=0,
        ReceivingContent=1,
        Decompressing=2,
        Decrypting = 3,
        Completed=4,
        Compressing=5,
        Encrypting=6,
        Transmitting=7,
        CompletedCompression=8,
        CompletedEncryption=9,
        CompletedDecompression=10,
        CompletedDecryption=11,
        Skipping=12,
        Idle=13

    }

}