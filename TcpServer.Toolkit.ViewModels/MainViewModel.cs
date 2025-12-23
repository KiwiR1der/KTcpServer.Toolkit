using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using KTcpServer.Toolkit.Core.Services;
using KTcpServer.Toolkit.Models;

namespace KTcpServer.Toolkit.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServerService _serverService;
        private const int Port = 10011;

        [ObservableProperty]
        private bool _isServerRunning;

        [ObservableProperty]
        private string _statusText = "服务已停止";

        [ObservableProperty]
        private ObservableCollection<string> _logMessages = new();

        [ObservableProperty]
        private ObservableCollection<ClientConnection> _connectedClients = new();


        [ObservableProperty]
        private ClientConnection? _selectedClient;
        [ObservableProperty]
        private string _messageToSend = "";

        public MainViewModel(IServerService serverService)
        {
            _serverService = serverService;

            Application.Current.Dispatcher.Invoke(() =>
            {
                LogMessages.Add("=== 这是一条测试日志，如果你能看到我，说明 ListView 没问题 ===");
            });

            // 订阅服务事件，更新UI
            _serverService.OnMessageLogged += msg =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
                });
            };
            _serverService.OnClientConnected += client =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectedClients.Add(client);
                });
            };
            _serverService.OnClientDisconnected += clientId =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var clientToRemove = ConnectedClients.FirstOrDefault(c => c.Id == clientId);
                    if (clientToRemove != null) ConnectedClients.Remove(clientToRemove);
                });
            };

            _serverService.OnDataReceived += (clientId, data) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var client = ConnectedClients.FirstOrDefault(c => c.Id == clientId);
                    LogMessages.Insert(0, $"[接收] 来自 {(client?.EndPoint ?? "未知客户端")}: {data}");
                });
            };

            IsServerRunning = false;
        }

        [RelayCommand]
        private async Task ToggleServerAsync()
        {
            if (IsServerRunning)
            {
                _serverService.Stop();
                IsServerRunning = false;
                StatusText = "服务器已停止";
            }
            else
            {
                StatusText = "正在启动服务器";

                // 这里的 await 会一直等待，直到 StartAsync 结束（也就是服务器停止）
                // await Task.Run(() => _serverService.StartAsync());

                // 后台线程运行，避免阻塞UI
                _ = Task.Run(() => _serverService.StartAsync());
                IsServerRunning = true;
                StatusText = $"服务器正在运行，监听端口 {Port}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanSend))]
        private async Task SendAsync()
        {
            if (SelectedClient != null)
            {
                await _serverService.SendToClientAsync(SelectedClient.Id, MessageToSend);
                MessageToSend = ""; // 清空输入框
            }
        }

        private bool CanSend() => IsServerRunning && SelectedClient != null && !string.IsNullOrEmpty(MessageToSend);


        // 当任意影响 CanSend 的条件改变时，需要通知 SendCommand 刷新起状态
        partial void OnIsServerRunningChanged(bool oldValue) => SendCommand.NotifyCanExecuteChanged();
        partial void OnSelectedClientChanged(ClientConnection? value) => SendCommand.NotifyCanExecuteChanged();
        partial void OnMessageToSendChanged(string? value) => SendCommand.NotifyCanExecuteChanged();

    }
}