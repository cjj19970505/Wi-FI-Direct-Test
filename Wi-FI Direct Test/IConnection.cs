using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Wi_FI_Direct_Test
{
    public enum ConnectionType { Bluetooth, UDP, TCP};
    public enum ConnectionEstablishState { Created = 0 ,Succeeded = 2, Failed = 6, Connecting = 1, Abort = 4, Disconnected = 5, Cancelled = 3}
    public delegate void MessageHandler(IConnection connection ,byte[] message);
    public delegate void ConnectionHandler(IConnection connection, ConnectionEstablishState connectionEstablishState);
    /// <summary>
    /// 这个是对所有类型连接的抽象
    /// 可以包括本地连接，TCP连接，蓝牙连接
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// 
        /// </summary>
        ConnectionType connectionType { get; }
        ConnectionEstablishState ConnectionEstablishState { get; }

        /// <summary>
        /// 改变状态
        /// </summary>
        event ConnectionHandler OnConnectionEstalblishResult;

        /// <summary>
        /// 收到信号时转到这个事件
        /// </summary>
        event MessageHandler onReceiveMessage;
        
        /// <summary>
        /// 传输数据
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendAsync(byte[] message);
        
    }

    public interface IClientConnection:IConnection
    {
        /// <summary>
        /// 建立连接（务必实现异步）
        /// 仅在连接成功时返回，否则一直阻塞
        /// </summary>
        Task<ConnectionEstablishState> ConnectAsync();

        /// <summary>
        /// 若迟迟没有返回连接成功的结果，可以调用这个中止连接
        /// </summary>
        void AbortConnecting();
        
    }

    public interface IServerConnection : IConnection
    {
        /// <summary>
        /// 开始广播
        /// </summary>
        void StartServer();

        /// <summary>
        /// 可以通过这串代码直接连接到这个Server
        /// </summary>
        string ConnectCode { get; }
    }


}
