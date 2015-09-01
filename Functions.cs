using System;
using System.Text;

namespace ProjetoBD2 {
    public static class Functions {
        public static byte[] GetBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public static string GetString(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }
    }
}

