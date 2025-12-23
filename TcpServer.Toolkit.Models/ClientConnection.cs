using System.Net.Sockets;

namespace KTcpServer.Toolkit.Models
{
    /// <summary>
    /// 已连接的客户端信息
    /// </summary>
    public class ClientConnection
    {
        public Guid Id { get; } = Guid.NewGuid();
        public TcpClient TcpClient { get; }
        public NetworkStream NetworkStream => TcpClient.GetStream();
        public string EndPoint => TcpClient.Client?.RemoteEndPoint?.ToString() ?? "UnKnown";

        public ClientConnection(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
        }

        public override string? ToString()
        {
            return $"Client {Id:N} ({EndPoint})";
        }
    }
}

