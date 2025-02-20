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
        private string LogFilePath;
        // 初始化日志记录
        public void InitializeLog()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
            // 确保日志目录存在
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // 创建日志文件路径
            LogFilePath = Path.Combine(logDirectory, "WindowsServiceAgentGUI.log");
        }
        // 日志事件
        public void Log(string message, EventLogType type)
        {
            // 写入日志文件
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, logEntry);
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入日志文件时出错：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
