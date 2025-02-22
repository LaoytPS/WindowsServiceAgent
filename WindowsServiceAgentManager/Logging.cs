using System;
using System.IO;
using System.Windows;

namespace WindowsServiceAgentManager
{
    public enum EventLogType 
    { 
        错误 = 1,
        警告 = 2,
        信息 = 4,
        调试 = 8,
    };
    public class Logging
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        private static readonly string LogPath = Path.Combine(LogDirectory, "WindowsServiceAgentGUI.log");
        // 初始化日志记录
        public void InitializeLog()
        {
            // 确保日志目录存在
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }
        // 日志事件
        public void Log(string message, EventLogType type)
        {
            // 写入日志文件
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, logEntry);
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入日志文件时出错：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
