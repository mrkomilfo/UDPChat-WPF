using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace lab1.Classes
{
    static class ByteParser
    {
        public static byte[] ObjToByteArray(object message)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, message);
                return ms.ToArray();
            }
        }

        public static object ByteArrayToObj(byte[] byteArray)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(byteArray, 0, byteArray.Length);
                ms.Seek(0, SeekOrigin.Begin);
                return new BinaryFormatter().Deserialize(ms);
            }                
        }
    }
}
