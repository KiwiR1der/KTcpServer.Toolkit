using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace KTcpServer.Toolkit.Core
{
    public static class ProcessLocker
    {
        private static Mutex ProcessLock = null;
        private static bool HasLocked = false;

        public static void GetProcessLock()
        {
            ProcessLock = new Mutex(false, $"TWPS09.Toolkit.Core.ProcessLocker.{GetUid()}", out HasLocked);

            if (!HasLocked)
            {
                ActiveWindow();
                Environment.Exit(0);
            }
        }

        public static void ActiveWindow()
        {
            using (var p = Process.GetCurrentProcess())
            {
                string pName = p.ProcessName;
                Process[] processes = Process.GetProcessesByName(pName);
                foreach (var process in processes)
                {
                    if (process?.MainModule?.FileName == p?.MainModule?.FileName)
                    {
                        IntPtr handle = process.MainWindowHandle;
                        SwitchToThisWindow(handle, true);
                        break;
                    }
                }
            }
        }

        [DllImport("user32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        private static string GetUid()
        {
            var bytes = Encoding.UTF8.GetBytes(Assembly.GetExecutingAssembly().Location);
            using (var md5 = MD5.Create())
            {
                bytes = md5.ComputeHash(bytes);
            }
            return BitConverter.ToString(bytes);
        }

        /// <summary>
        /// 释放当前进程的锁
        /// </summary>
        /// <remarks>小心使用</remarks>
        public static void ReleaseLock()
        {
            if (ProcessLock != null && HasLocked)
            {
                ProcessLock.Dispose();
                HasLocked = false;
            }
        }
    }
}
