namespace SimpleSockets.Messaging {

    public enum MessageState {

        Receiving=0,
        Decompressing=1,
        Decrypting = 2,
        Completed=3,
        Compressing=4,
        Encrypting=5,
        Transmitting=6,
        CompletedCompression=7,
        CompletedEncryption=8,
        CompletedDecompression=9,
        CompletedDecryption=10

    }

}