using System;

namespace UnDKC3
{
    static class Utils
    {
        public static int GetUShort(byte[] Data, int Index)
        {
            return
                (Data[Index + 0] << 0) |
                (Data[Index + 1] << 8);
        }

        public static string[] ToLines(string Data)
        {
            return Data.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        }
    }
}
