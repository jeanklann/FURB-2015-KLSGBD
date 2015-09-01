using System;

namespace ProjetoBD2 {
    public static class Functions {
        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length];
            for(int i = 0; i < bytes.Length; i++) {
                bytes[i] = (byte)str[i];
            }
            return bytes;
        }

        public static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length];
            Array.Copy(bytes, chars, bytes.Length);
            return new string(chars);
        }
    }
}

