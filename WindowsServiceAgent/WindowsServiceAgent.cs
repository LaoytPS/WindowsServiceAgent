using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Management;

namespace WindowsServiceAgent
{
    public partial class WindowsServiceAgent : ServiceBase
    {
        // 程序内存储变量
        private string executablePath;
        private string arguments;
        private string workingDirectory;
        private Process AgentProcess;
        private FileSystemWatcher configWatcher;
        private string LogFilePath;

        // 服务初始化
        public WindowsServiceAgent()
        {
            InitializeComponent();
            this.ServiceName = InitializeServiceName();
            InitializeLog();
        }

        // 初始化服务名称
        private string InitializeServiceName()
        {
            // 获取命令行参数（包括可执行文件路径）
            string[] Args = Environment.GetCommandLineArgs();

            // 移除第一个元素（可执行文件路径），获取实际的启动参数
            string[] args = Args.Skip(1).ToArray();

            // 默认服务名称
            string serviceName = "No Args";

            // 解析启动参数
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "-sn" || args[i] == "--servicename") && i + 1 < args.Length)
                {
                    serviceName = args[i + 1];
                    i++; // 跳过已处理的参数
                }
            }
            return serviceName;
        }

        // 初始化日志记录
        private void InitializeLog()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
            // 确保日志目录存在
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // 使用服务名称来命名日志文件
            string serviceName = this.ServiceName;
            LogFilePath = Path.Combine(logDirectory, $"{serviceName}_Output.log");
        }

        // 初始化配置文件监视器
        private void InitializeConfigWatcher(string configFilePath)
        {
            configWatcher = new FileSystemWatcher(Path.GetDirectoryName(configFilePath))
            {
                Filter = Path.GetFileName(configFilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            configWatcher.Changed += OnConfigChanged;
            configWatcher.EnableRaisingEvents = true;
        }

        // 日志记录
        private void Log(string message, EventLogEntryType type)
        {
            // 写入事件日志
            EventLog.WriteEntry(message, type);

            // 写入日志文件
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, logEntry);
            }
            catch (Exception ex)
            {
                // 如果写入日志文件时出错，可以记录到事件日志，但避免递归调用
                EventLog.WriteEntry($"写入日志文件时出错：{ex.Message}", EventLogEntryType.Error);
            }
        }

        // 加载服务配置
        private void LoadConfiguration(string serviceName)
        {
            // 设置配置文件路径
            string configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceConfigs");
            string configFilePath = Path.Combine(configDirectory, $"{serviceName}.json");
            Log("正在尝试加载配置文件。", EventLogEntryType.Information);

            // 确保日志目录存在
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            // 尝试查找配置文件
            if (!File.Exists(configFilePath))
            {
                // 日志记录或错误处理
                Log($"配置文件 {configFilePath} 未找到，请确保应用运行目录下有ServiceConfigs文件夹，以及同服务名称的.json文件", EventLogEntryType.Error);
                this.ExitCode = 1064; // ERROR_SERVICE_NO_THREAD
                Stop();
                return;
            }

            try
            {
                // 读取配置文件
                string configJson = File.ReadAllText(configFilePath);
                ServiceConfig config = JsonConvert.DeserializeObject<ServiceConfig>(configJson);

                // 保存配置信息
                executablePath = config.ExecutablePath;
                arguments = config.Arguments;
                workingDirectory = config.WorkingDirectory;
            }
            catch (Exception ex)
            {
                Log($"加载配置文件时出错：{ex.Message}\r\n请检查软件是否拥有对配置文件读取的权限，以及是否以管理员身份运行此程序。", EventLogEntryType.Error);
                this.ExitCode = 1064;
                Stop();
            }
        }

        // 尝试启动被代理的应用程序
        private void StartApplication()
        {
            Log("正在尝试启动目标应用程序。", EventLogEntryType.Information);
            // 检测应用路径是否为空
            if (string.IsNullOrEmpty(executablePath))
            {
                Log("应用程序路径未配置。", EventLogEntryType.Error);
                this.ExitCode = 1064;
                Stop();
                return;
            }

            // 在代理应用内保存进程信息
            AgentProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(executablePath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };

            try
            {
                AgentProcess.Start();
            }
            catch (Exception ex)
            {
                Log($"启动被代理应用程序时出错：{ex.Message}", EventLogEntryType.Error);
                this.ExitCode = 1064;
                Stop();
            }

            AgentProcess.Exited += OnAgentProcessExited;
            
        }

        // 服务启动时
        protected override void OnStart(string[] args)
        {
            // 获取当前服务的名称
            string serviceName = this.ServiceName;

            // 加载配置
            LoadConfiguration(serviceName);

            // 启动被代理的应用程序
            StartApplication();

            // 启动配置文件监视器
            InitializeConfigWatcher(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"ServiceConfigs\{serviceName}.json"));
        }

        // 服务停止时
        protected override void OnStop()
        {
            if (AgentProcess != null && !AgentProcess.HasExited)
            {
                try
                {
                    AgentProcess.Kill();
                    AgentProcess.WaitForExit();
                }
                catch (Exception ex)
                {
                    Log($"停止被代理应用程序时出错：{ex.Message}", EventLogEntryType.Error);
                }
                finally
                {
                    AgentProcess.Dispose();
                    AgentProcess = null;
                }
            }
        }

        // 代理应用程序退出时
        private void OnAgentProcessExited(object sender, EventArgs e)
        {
            Log("被代理应用程序已退出。已对Windows服务控制管理器发送关闭服务命令", EventLogEntryType.Warning);
            Stop();
        }

        // 配置文件更改时
        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            Log("当前服务配置文件已被更改，已尝试关闭之前代理的应用程序，正在重新加载配置并启动", EventLogEntryType.Warning);
            // 停止当前的被代理应用程序
            OnStop();
            // 重新加载配置并启动
            LoadConfiguration(this.ServiceName);
            StartApplication();
        }
        
    }
}