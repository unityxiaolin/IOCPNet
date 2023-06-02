using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// IOCP连接会话Session
/// </summary>
namespace XLGame
{
    public enum SessionState {
        None,
        Conected,
        DisConnected
    }

    public abstract class IOCPSession<T> where T : IOCPMsg, new() {
        public int m_sessionID;
        private SocketAsyncEventArgs m_receiveEventArgs;
        private SocketAsyncEventArgs m_sendEventArgs;

        private Socket m_socket;
        private List<byte> m_readList = new List<byte>();
        private Queue<byte[]> m_cacheQueue = new Queue<byte[]>();
        private bool isWrite = false;
        public Action<int> OnSessionClose;
        public SessionState m_sessionState = SessionState.None;

        public IOCPSession() {
            m_receiveEventArgs = new SocketAsyncEventArgs();
            m_sendEventArgs = new SocketAsyncEventArgs();
            m_receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            m_sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            m_receiveEventArgs.SetBuffer(new byte[2048], 0, 2048);
        }

        public void InitSession(Socket socket) {
            m_socket = socket;
            m_sessionState = SessionState.Conected;
            OnConnected();
            StartAsyncReceive();
        }

        void StartAsyncReceive() {
            bool suspend = m_socket.ReceiveAsync(m_receiveEventArgs);
            if(suspend == false) {
                ProcessReceive();
            }
        }
        void ProcessReceive() {
            if(m_receiveEventArgs.BytesTransferred > 0 && m_receiveEventArgs.SocketError == SocketError.Success) {
                byte[] bytes = new byte[m_receiveEventArgs.BytesTransferred];
                Buffer.BlockCopy(m_receiveEventArgs.Buffer, 0, bytes, 0, m_receiveEventArgs.BytesTransferred);
                m_readList.AddRange(bytes);
                ProcessByteList();
                StartAsyncReceive();
            }
            else {
                IOCPTool.Warn("SessionID:{0} SocketError:{1}", m_sessionID, m_receiveEventArgs.SocketError.ToString());
                CloseSession();
            }
        }
        void ProcessByteList() {
            byte[] buff = IOCPTool.SplitLogicBytes(ref m_readList);
            if(buff != null) {
                T msg = IOCPTool.DeSerialize<T>(buff);
                OnReceiveMsg(msg);
                ProcessByteList();
            }
        }

        public bool SendMsg(T msg) {
            byte[] bytes = IOCPTool.PackLenInfo(IOCPTool.Serialize(msg));
            return SendMsg(bytes);
        }
        public bool SendMsg(byte[] bytes) {
            if(m_sessionState != SessionState.Conected) {
                IOCPTool.Warn("Connection is break,cannot send net msg.");
                return false;
            }
            if(isWrite) {
                m_cacheQueue.Enqueue(bytes);
                return true;
            }
            isWrite = true;
            m_sendEventArgs.SetBuffer(bytes, 0, bytes.Length);
            bool suspend = m_socket.SendAsync(m_sendEventArgs);
            if(suspend == false) {
                ProcessSend();
            }
            return true;
        }
        void ProcessSend() {
            if(m_sendEventArgs.SocketError == SocketError.Success) {
                isWrite = false;
                if(m_cacheQueue.Count > 0) {
                    byte[] data = m_cacheQueue.Dequeue();
                    SendMsg(data);
                }
            }
            else {
                IOCPTool.Error("Process Send Error:{0}", m_sendEventArgs.SocketError.ToString());
                CloseSession();
            }
        }

        void IO_Completed(object sender, SocketAsyncEventArgs socketAsyncEventArgs) {
            switch(socketAsyncEventArgs.LastOperation) {
                case SocketAsyncOperation.Receive:
                    ProcessReceive();
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend();
                    break;
                default:
                    IOCPTool.Warn("The last operation completed on the socket was not a receive or send op.");
                    break;
            }
        }

        public void CloseSession() {
            if(m_socket != null) {
                m_sessionState = SessionState.DisConnected;
                OnDisConnected();
                OnSessionClose?.Invoke(m_sessionID);
                m_readList.Clear();
                m_cacheQueue.Clear();
                isWrite = false;

                try {
                    m_socket.Shutdown(SocketShutdown.Send);
                }
                catch(Exception e) {
                    IOCPTool.Error("Shutdown Socket Error:{0}", e.ToString());
                }
                finally {
                    m_socket.Close();
                    m_socket = null;
                }
            }
        }

        protected abstract void OnConnected();
        protected abstract void OnReceiveMsg(T msg);
        protected abstract void OnDisConnected();
    }
}
