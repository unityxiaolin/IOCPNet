using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Threading;

/// <summary>
/// IOCP工具类
/// </summary>
namespace XLGame {
    public static class IOCPTool {
        public static byte[] SplitLogicBytes(ref List<byte> bytesList) {
            byte[] buff = null;
            if(bytesList.Count > 4) {
                byte[] data = bytesList.ToArray();
                int len = BitConverter.ToInt32(data, 0);
                if(bytesList.Count >= len + 4) {
                    buff = new byte[len];
                    Buffer.BlockCopy(data, 4, buff, 0, len);
                    bytesList.RemoveRange(0, len + 4);
                }
            }
            return buff;
        }

        public static byte[] PackLenInfo(byte[] body) {
            int len = body.Length;
            byte[] pkg = new byte[len + 4];
            byte[] head = BitConverter.GetBytes(len);
            head.CopyTo(pkg, 0);
            body.CopyTo(pkg, 4);
            return pkg;
        }

        public static byte[] Serialize<T>(T msg) where T : IOCPMsg {
            byte[]? data = null;
            MemoryStream memoryStream = new MemoryStream();
            //BinaryFormatter binaryFormatter = new BinaryFormatter();
            try {
                //binaryFormatter.Serialize(memoryStream, msg);
                JsonSerializer.Serialize(memoryStream, msg);
                memoryStream.Seek(0, SeekOrigin.Begin);
                data = memoryStream.ToArray();
            }
            catch(SerializationException e) {
                Error("Faild to serialize,Reson:{0}", e.Message);
            }
            finally {
                memoryStream.Close();
            }
            return data;
        }

        public static T DeSerialize<T>(byte[] bytes) where T : IOCPMsg {
            T? msg = null;
            MemoryStream memoryStream = new MemoryStream(bytes);
            //BinaryFormatter binaryFormatter = new BinaryFormatter();
            try {
                //msg = (T)binaryFormatter.Deserialize(memoryStream);
                msg = JsonSerializer.Deserialize<T>(memoryStream);
            }
            catch(SerializationException e) {
                Error("Faild to deserialize.Reson:{0} bytesLen:{1}", e.Message, bytes.Length);
            }
            finally {
                memoryStream.Close();
            }
            return msg;
        }

        #region LOG
        public static Action<string> LogFunc;
        public static Action<IOCPLogColor, string> ColorLogFunc;
        public static Action<string> WarnFunc;
        public static Action<string> ErrorFunc;

        public static void Log(string msg, params object[] args) {
            msg = string.Format(msg, args);
            if(LogFunc != null) {
                LogFunc(msg);
            }
            else {
                ConsoleLog(msg, IOCPLogColor.None);
            }
        }
        public static void ColorLog(IOCPLogColor color, string msg, params object[] args) {
            msg = string.Format(msg, args);
            if(ColorLogFunc != null) {
                ColorLogFunc(color, msg);
            }
            else {
                ConsoleLog(msg, color);
            }
        }
        public static void Warn(string msg, params object[] args) {
            msg = string.Format(msg, args);
            if(WarnFunc != null) {
                WarnFunc(msg);
            }
            else {
                ConsoleLog(msg, IOCPLogColor.Yellow);
            }
        }
        public static void Error(string msg, params object[] args) {
            msg = string.Format(msg, args);
            if(ErrorFunc != null) {
                ErrorFunc(msg);
            }
            else {
                ConsoleLog(msg, IOCPLogColor.Red);
            }
        }
        private static void ConsoleLog(string msg, IOCPLogColor color) {
            int threadID = Thread.CurrentThread.ManagedThreadId;
            msg = string.Format("Thread:{0} {1}", threadID, msg);
            switch(color) {
                case IOCPLogColor.Red:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case IOCPLogColor.Green:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case IOCPLogColor.Blue:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case IOCPLogColor.Cyan:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case IOCPLogColor.Magenta:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case IOCPLogColor.Yellow:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case IOCPLogColor.None:
                default:
                    Console.WriteLine(msg);
                    break;
            }
        }
        #endregion
    }

    public enum IOCPLogColor {
        None,
        Red,
        Green,
        Blue,
        Cyan,
        Magenta,
        Yellow
    }
}
