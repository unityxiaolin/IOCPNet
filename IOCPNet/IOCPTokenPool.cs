using System.Collections.Generic;

/// <summary>
/// IOCP会话连接Session缓存池
/// </summary>
namespace XLGame
{
    public class IOCPSessionPool<T, K>
        where T : IOCPSession<K>, new()
        where K : IOCPMsg, new() {
        Stack<T> m_sessionStack;
        public int Size => m_sessionStack.Count;

        public IOCPSessionPool(int capacity) {
            m_sessionStack = new Stack<T>(capacity);
        }

        public T Pop() {
            lock(m_sessionStack) {
                return m_sessionStack.Pop();
            }
        }

        public void Push(T Session) {
            if(Session == null) {
                IOCPTool.Error("push Session to pool cannot be null");
            }
            lock(m_sessionStack) {
                m_sessionStack.Push(Session);
            }
        }
    }
}
