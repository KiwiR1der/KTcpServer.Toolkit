using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using KTcpServer.Toolkit.Core;
using KTcpServer.Toolkit.Core.Services;
using KTcpServer.Toolkit.ViewModels;

namespace KTcpServer.Toolkit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        public App()
        {
            ProcessLocker.GetProcessLock();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
            {
                // 注册核心服务
                //services.AddSingleton<IServerService, TcpServer>();
                services.AddSingleton<IServerService>(sp => new TcpServer(10011));

                // 注册ViewModels
                services.AddSingleton<MainViewModel>();

                // 注册Views
                services.AddSingleton<MainWindow>();
            }).Build();

            // 启动并显示主窗口
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose(); // 清理资源，包括调用TcpServer的Dispose
            base.OnExit(e);
        }
    }
}