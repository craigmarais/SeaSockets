using System.Runtime.InteropServices;

namespace CSockets
{
    public static class Utilities
    {
        public static byte[] AddLength(byte[] buffer)
        {
            var bytesToSend = new byte[4 + buffer.Length];
            Span<int> length = new int[] { buffer.Length };
            var lengthBytes = MemoryMarshal.Cast<int, byte>(length);
            bytesToSend[0] = lengthBytes[0];
            bytesToSend[1] = lengthBytes[1];
            bytesToSend[2] = lengthBytes[2];
            bytesToSend[3] = lengthBytes[3];
            buffer.CopyTo(bytesToSend, 4);
            return bytesToSend;
        }
    }
}
