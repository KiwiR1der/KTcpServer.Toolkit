using KTcpServer.Toolkit.Models;

namespace KTcpServer.Toolkit.Core.Services
{
    /// <summary>
    /// “服务提供者”，定义服务行为
    /// </summary>
    public interface IServerService
    {
        bool IsRunning { get; }
        Task StartAsync();
        void Stop();
        Task SendToClientAsync(Guid clientId, string message);
        IEnumerable<ClientConnection> GetConnectClients();

        // 事件
        event Action<string>? OnMessageLogged;
        event Action<ClientConnection>? OnClientConnected;
        event Action<Guid>? OnClientDisconnected;
        event Action<Guid, string>? OnDataReceived;
    }
}