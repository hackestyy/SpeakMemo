using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace SpeakMemo
{
    public class Common
    {
        public static UInt16[] BytesArrayToUint16Array(byte[] b)
        {
            UInt16[] u = new UInt16[b.Length/2];
            for (int i = 0; i < u.Length; i++)
            {
                u[i] = BitConverter.ToUInt16(b, i * 2);
            }

            return u;
        }
        public static byte[] Uint16ArrayToBytesArray(Int16[] u)
        {
            byte[] b = new byte[2 * u.Length];
            for (int i = 0; i < u.Length; i++)
            {
                b[i*2] = BitConverter.GetBytes(u[i])[0];
                b[i * 2+1] = BitConverter.GetBytes(u[i])[1];
            }
            return b;
        }
        public static byte[] ByteArrayListToByteArray(List<byte[]> bl)
        {
            int length = 0;
            int resultIndex = 0;
            foreach (byte[] b in bl)
            {
                length += b.Length;
            }
            byte[] result = new byte[length];
            foreach (byte[] b in bl)
            {
                
                for (int i = 0; i < b.Length; i++)
                {
                    result[resultIndex+i] = b[i];
                }
                resultIndex += b.Length;
            }
            return result;
        }
        public static string ByteArrayToString(byte[] br)
        {
            String s = "";
            foreach (byte b in br)
            {
                s =s + b.ToString()+",";
            }
            return s;
        }
    }
}
