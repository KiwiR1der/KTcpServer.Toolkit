using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using KTcpServer.Toolkit.Models;

namespace KTcpServer.Toolkit.Core.Services
{
    public class TcpServer : IDisposable, IServerService
    {
        private readonly TcpListener _listener;
        private CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<Guid, ClientConnection> _clients = new();

        // 事件，用于通知上层（ViewModel）状态变化
        public event Action<string>? OnMessageLogged;
        public event Action<ClientConnection>? OnClientConnected;
        public event Action<Guid>? OnClientDisconnected;
        public event Action<Guid, string>? OnDataReceived;

        public bool IsListening { get; private set; }
        public int Port { get; set; }

        // IServerService 
        public bool IsRunning => IsListening;
        public IEnumerable<ClientConnection> GetConnectClients() => _clients.Values;

        public TcpServer(int port)
        {
            Port = port;
            // 监听所有网络接口的指定端口
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public async Task StartAsync()
        {
            if (IsListening) return;
            _listener.Start();
            IsListening = true;

            OnMessageLogged?.Invoke($"服务器已启动，正在监听端口 {Port}...");

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await HandleClientAsync(tcpClient);
                            }
                            catch (Exception ex)
                            {

                                throw;
                            }

                        });
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常退出
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                OnMessageLogged?.Invoke($"监听异常：{ex.Message}");
            }
            finally
            {
                Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            var client = new ClientConnection(tcpClient);
            _clients.TryAdd(client.Id, client);
            OnClientConnected?.Invoke(client);
            OnMessageLogged?.Invoke($"客户端 {client.EndPoint} 已连接。");
            var buffer = new byte[1024];    // 1KB 缓冲区
            try
            {
                while (!_cts.Token.IsCancellationRequested && client.TcpClient.Connected)
                {
                    int bytesRead = await client.NetworkStream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (bytesRead == 0)
                    {
                        // 客户端已断开连接
                        break;
                    }
                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnDataReceived?.Invoke(client.Id, data);
                }
            }
            catch (Exception ex)
            {
                OnMessageLogged?.Invoke($"与客户端 {client.EndPoint} 通信时发生异常：{ex.Message}");
            }
            finally
            {
                DisconnectClient(client.Id);
            }
        }

        private void DisconnectClient(Guid clientId)
        {
            if (_clients.TryRemove(clientId, out var client))
            {
                client.TcpClient.Close();
                OnClientDisconnected?.Invoke(clientId);
                OnMessageLogged?.Invoke($"客户端 {client.EndPoint} 已断开。");
            }
        }

        public async Task SendToClientAsync(Guid clientId, string message)
        {
            if (_clients.TryGetValue(clientId, out var client) && client.TcpClient.Connected)
            {
                var data = Encoding.UTF8.GetBytes(message);
                await client.NetworkStream.WriteAsync(data, 0, data.Length);
                OnMessageLogged?.Invoke($"已向 {client.EndPoint} 发送：{message}");
            }
            else
            {
                OnMessageLogged?.Invoke($"发送失败：客户端 {clientId} 未找到或已断开连接。");
            }
        }

        public void Stop()
        {
            if ((!IsListening)) return;

            _cts.Cancel();
            _listener.Stop();
            IsListening = false;
            // 断开所有客户端连接
            foreach (var clientId in _clients.Keys)
            {
                DisconnectClient(clientId);
            }

            OnMessageLogged?.Invoke("服务器已停止。");
            _cts = new();
        }

        public void Dispose()
        {
            Stop();
            _cts.Dispose();
            _listener.Dispose();
        }
    }
}
