using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

/// <summary>
/// 基于IOCP封装的异步套接字通信
/// </summary>
namespace XLGame
{
    [Serializable]
    public abstract class IOCPMsg { }

    public class IOCPNet<T, K>
        where T : IOCPSession<K>, new()
        where K : IOCPMsg, new() {
        Socket m_socket;
        SocketAsyncEventArgs m_socketAsyncEventArgs;
        public IOCPNet() {
            m_socketAsyncEventArgs = new SocketAsyncEventArgs();
            m_socketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
        }

        #region Client

        public T m_session;
        public void StartAsClient(string ip, int port) {
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            m_socket = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            m_socketAsyncEventArgs.RemoteEndPoint = iPEndPoint;
            IOCPTool.ColorLog(IOCPLogColor.Green, "Client Start...");
            StartConnect();
        }
        void StartConnect() {
            bool suspend = m_socket.ConnectAsync(m_socketAsyncEventArgs);
            if(suspend == false) {
                ProcessConnect();
            }
        }
        void ProcessConnect() {
            m_session = new T();
            m_session.InitSession(m_socket);
        }
        public void ClosetClient() {
            if(m_session != null) {
                m_session.CloseSession();
                m_session = null;
            }
            m_socket = null;
        }
        #endregion

        #region Server
        int m_curConnectCount = 0;
        public int m_backlog = 100;
        Semaphore m_acceptSeamaphore;
        IOCPSessionPool<T, K> m_sessionPool;
        List<T> m_sessionList;
        public void StartAsServer(string ip, int port, int maxConnCount) {
            m_curConnectCount = 0;
            m_acceptSeamaphore = new Semaphore(maxConnCount, maxConnCount);
            m_sessionPool = new IOCPSessionPool<T, K>(maxConnCount);
            for(int i = 0; i < maxConnCount; i++) {
                T Session = new T {
                    m_sessionID = i
                };
                m_sessionPool.Push(Session);
            }
            m_sessionList = new List<T>();

            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            m_socket = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            m_socket.Bind(iPEndPoint);
            m_socket.Listen(m_backlog);
            IOCPTool.ColorLog(IOCPLogColor.Green, "Server Start...");
            StartAccept();
        }
        void StartAccept() {
            m_socketAsyncEventArgs.AcceptSocket = null;
            m_acceptSeamaphore.WaitOne();
            bool suspend = m_socket.AcceptAsync(m_socketAsyncEventArgs);
            if(suspend == false) {
                ProcessAccept();
            }
        }
        void ProcessAccept() {
            Interlocked.Increment(ref m_curConnectCount);
            T Session = m_sessionPool.Pop();
            lock(m_sessionList) {
                m_sessionList.Add(Session);
            }
            Session.InitSession(m_socketAsyncEventArgs.AcceptSocket);
            Session.OnSessionClose = OnSessionClose;
            IOCPTool.ColorLog(IOCPLogColor.Green, "Client Online,Allocate SessionID:{0}", Session.m_sessionID);
            StartAccept();
        }
        void OnSessionClose(int SessionID) {
            int index = -1;
            for(int i = 0; i < m_sessionList.Count; i++) {
                if(m_sessionList[i].m_sessionID == SessionID) {
                    index = i;
                    break;
                }
            }
            if(index != -1) {
                m_sessionPool.Push(m_sessionList[index]);
                lock(m_sessionList) {
                    m_sessionList.RemoveAt(index);
                }
                Interlocked.Decrement(ref m_curConnectCount);
                m_acceptSeamaphore.Release();
            }
            else {
                IOCPTool.Error("Session:{0} cannot find in server SessionList.", SessionID);
            }
        }
        public void CloseServer() {
            for(int i = 0; i < m_sessionList.Count; i++) {
                m_sessionList[i].CloseSession();
            }
            m_sessionList = null;
            if(m_socket != null) {
                m_socket.Close();
                m_socket = null;
            }
        }
        public List<T> GetSessionList() {
            return m_sessionList;
        }
        #endregion

        void IO_Completed(object sender, SocketAsyncEventArgs socketAsyncEventArgs) {
            switch(socketAsyncEventArgs.LastOperation) {
                case SocketAsyncOperation.Accept:
                    ProcessAccept();
                    break;
                case SocketAsyncOperation.Connect:
                    ProcessConnect();
                    break;
                default:
                    IOCPTool.Warn("The last operation completed on the socket was not a accept or connect op.");
                    break;
            }
        }
    }
}
