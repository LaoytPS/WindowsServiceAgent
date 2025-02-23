﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace WindowsServiceAgent
{
    public partial class WindowsServiceAgent : ServiceBase
    {
        // 程序内存储变量
        private string executablePath;
        private string arguments;
        private string workingDirectory;
        private string ConfigName;
        private Process AgentProcess;
        private FileSystemWatcher configWatcher;
        private string LogFilePath;
        private enum EventLogType
        {
            错误 = 1,
            警告 = 2,
            信息 = 4,
            调试 = 8,
        };

        // 服务初始化
        public WindowsServiceAgent()
        {
            InitializeComponent();
            ConfigName = InitializeConfigName();
            InitializeLog();
        }

        // 初始化配置文件名称
        private string InitializeConfigName()
        {
            // 获取命令行参数（包括可执行文件路径）并移除第一个元素（可执行文件路径），获取实际的启动参数
            string[] Args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            // 默认配置文件名称
            string ConfigName = "No_Args";

            // 解析启动参数
            for (int i = 0; i < Args.Length; i++)
            {
                if ((Args[i] == "-c" || Args[i] == "--configname") && i + 1 < Args.Length)
                {
                    ConfigName = Args[i + 1];
                    i++; // 跳过已处理的参数
                }
            }
            return ConfigName;
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

            // 使用配置文件名称来命名日志文件
            LogFilePath = Path.Combine(logDirectory, $"{ConfigName}_Output.log");
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
        private void Log(string message, EventLogType type)
        {
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
        private void LoadConfiguration()
        {
            // 设置配置文件路径
            string configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceConfigs");
            string configFilePath = Path.Combine(configDirectory, $"{ConfigName}.json");
            Log("正在尝试加载配置文件。", EventLogType.信息);

            // 确保配置文件目录存在
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            // 尝试查找配置文件
            if (!File.Exists(configFilePath))
            {
                // 日志记录或错误处理
                Log($"配置文件 {configFilePath} 未找到，请确保应用运行目录下有ServiceConfigs文件夹，以及同服务名称的.json文件", EventLogType.错误);
                this.ExitCode = 1064; // ERROR_SERVICE_NO_THREAD
                Stop();
                return;
            }

            try
            {
                // 读取配置文件
                string configJson = File.ReadAllText(configFilePath);

                // 自定义json解析
                Dictionary<string, string> config = ParseJson(configJson);

                // 保存配置信息
                if (config.TryGetValue("ExecutablePath", out string execPath))
                    executablePath = execPath;
                else
                {
                    Log("配置文件中缺少 ExecutablePath 项。", EventLogType.错误);
                    this.ExitCode = 1064;
                    Stop();
                    return;
                }
                arguments = config.ContainsKey("Arguments") ? config["Arguments"] : "";
                workingDirectory = config.ContainsKey("WorkingDirectory") ? config["WorkingDirectory"] : null;
            }
            catch (Exception ex)
            {
                Log($"加载配置文件时出错：{ex.Message}\r\n请检查软件是否拥有对配置文件读取的权限，以及是否以管理员身份运行此程序。", EventLogType.错误);
                this.ExitCode = 1064;
                Stop();
            }
        }

        // JSON 解析函数
        private Dictionary<string, string> ParseJson(string json)
        {
            var result = new Dictionary<string, string>();

            // 匹配 "键": "值" 的模式
            var matches = Regex.Matches(json, @"""([^""]+)""\s*:\s*""([^""]*)""");
            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                var value = UnescapeJsonString(match.Groups[2].Value);
                result[key] = value;
            }

            return result;
        }

        // 处理 JSON 字符串中的转义字符
        private string UnescapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace("\\\"", "\"")
                        .Replace("\\\\", "\\");
        }

        // 尝试启动被代理的应用程序
        private void StartApplication()
        {
            Log("正在尝试启动目标应用程序。", EventLogType.信息);
            // 检测应用路径是否为空
            if (string.IsNullOrEmpty(executablePath))
            {
                Log("应用程序路径未配置。", EventLogType.错误);
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
                Log($"启动被代理应用程序时出错：{ex.Message}", EventLogType.错误);
                this.ExitCode = 1064;
                Stop();
            }

            AgentProcess.Exited += OnAgentProcessExited;

        }

        // 服务启动时
        protected override void OnStart(string[] args)
        {
            // 加载配置
            LoadConfiguration();

            // 启动被代理的应用程序
            StartApplication();

            // 启动配置文件监视器
            InitializeConfigWatcher(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"ServiceConfigs\{ConfigName}.json"));
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
                    Log($"停止被代理应用程序时出错：{ex.Message}", EventLogType.错误);
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
            Log("被代理应用程序已退出。已对Windows服务控制管理器发送关闭服务命令", EventLogType.警告);
            Stop();
        }

        // 配置文件更改时
        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            Log("当前服务配置文件已被更改，已尝试关闭之前代理的应用程序，正在重新加载配置并启动", EventLogType.警告);
            // 停止当前的被代理应用程序
            OnStop();
            // 重新加载配置并启动
            LoadConfiguration();
            StartApplication();
        }

    }
}