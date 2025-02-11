using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using ServiceLibrary;


namespace WindowsServiceAgent
{
    public partial class WindowsServiceAgent : ServiceBase
    {
        //服务构造函数
        public WindowsServiceAgent()
        {
            InitializeComponent();
            //设置默认服务名称
            this.ServiceName = "WindowsServiceAgent";
        }

        //命名管道名称
        private const string PipeName = "WindowsServiceAgent_Pipe";
        private CancellationTokenSource cancellationTokenSource;

        //创建命名管道服务器
        private void StartPipeServer()
        {
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ListenForClients(cancellationTokenSource.Token), cancellationTokenSource.Token);
        }

        //异步等待客户端连接
        private async Task ListenForClients(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                {
                    await pipeServer.WaitForConnectionAsync(token);

                    if (pipeServer.IsConnected)
                    {
                        // 处理客户端请求
                        await HandleClientAsync(pipeServer);
                    }
                }
            }
        }

        //处理客户端请求
        private async Task HandleClientAsync(NamedPipeServerStream pipeStream)
        {
            try
            {
                using (StreamReader reader = new StreamReader(pipeStream))
                using (StreamWriter writer = new StreamWriter(pipeStream) { AutoFlush = true })
                {
                    // 读取请求
                    string request = await reader.ReadLineAsync();
                    // 处理请求并获取响应
                    string response = ProcessRequest(request);
                    // 发送响应
                    await writer.WriteLineAsync(response);
                }
            }
            catch (Exception ex)
            {
                // 处理异常，记录日志
                EventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }

        //处理进程请求
        private string ProcessRequest(string requestJson)
        {
            try
            {
                // 解析请求
                var request = JsonConvert.DeserializeObject<ServiceRequest>(requestJson);

                switch (request.Command)
                {
                    case "Start":
                        StartManagedProcess(request);
                        return "Service started.";
                    case "Stop":
                        StopManagedProcess(request.ServiceName);
                        return "Service stopped.";
                    case "Status":
                        var status = GetServiceStatus(request.ServiceName);
                        return JsonConvert.SerializeObject(status);
                    default:
                        return "Unknown command.";
                }
            }
            catch (Exception ex)
            {
                // 记录异常
                return $"Error: {ex.Message}";
            }
        }


        //创建服务状态类
        public class ServiceStatus
        {
            public string ServiceName { get; set; }
            public bool IsRunning { get; set; }
            public int PID { get; set; }
            public List<int> Ports { get; set; }
        }

        //创建字典保存进程信息
        private Dictionary<string, Process> managedProcesses = new Dictionary<string, Process>();

        //启动进程
        private void StartManagedProcess(ServiceRequest request)
        {
            if (managedProcesses.ContainsKey(request.ServiceName))
            {
                // 服务已在运行
                return;
            }

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = request.ExecutablePath,
                    Arguments = request.Arguments,
                    WorkingDirectory = request.WorkingDirectory ?? Path.GetDirectoryName(request.ExecutablePath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            process.Exited += (s, e) =>
            {
                managedProcesses.Remove(request.ServiceName);
                // 记录日志或通知 GUI
            };

            process.Start();

            managedProcesses.Add(request.ServiceName, process);

            // 记录日志或通知 GUI
        }

        //停止进程
        private void StopManagedProcess(string serviceName)
        {
            if (managedProcesses.TryGetValue(serviceName, out Process process))
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                    process.Dispose();
                }
                managedProcesses.Remove(serviceName);
            }
        }

        //获取服务状态
        private ServiceStatus GetServiceStatus(string serviceName)
        {
            if (managedProcesses.TryGetValue(serviceName, out Process process))
            {
                var status = new ServiceStatus
                {
                    ServiceName = serviceName,
                    IsRunning = !process.HasExited,
                    PID = process.Id,
                    Ports = GetProcessPorts(process.Id)
                };
                return status;
            }
            else
            {
                return new ServiceStatus
                {
                    ServiceName = serviceName,
                    IsRunning = false,
                    PID = 0,
                    Ports = new List<int>()
                };
            }
        }

        //获取进程占用端口
        private List<int> GetProcessPorts(int pid)
        {
            List<int> ports = new List<int>();

            // 使用 netstat 命令获取端口信息
            try
            {
                Process netstatProcess = new Process();
                netstatProcess.StartInfo.FileName = "netstat.exe";
                netstatProcess.StartInfo.Arguments = "-ano";
                netstatProcess.StartInfo.UseShellExecute = false;
                netstatProcess.StartInfo.RedirectStandardOutput = true;
                netstatProcess.StartInfo.CreateNoWindow = true;
                netstatProcess.Start();

                string output = netstatProcess.StandardOutput.ReadToEnd();
                netstatProcess.WaitForExit();

                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    if (line.StartsWith("  TCP") || line.StartsWith("  UDP"))
                    {
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5)
                        {
                            string localAddress = parts[1];
                            string pidStr = parts[parts.Length - 1];
                            if (int.TryParse(pidStr, out int entryPid) && entryPid == pid)
                            {
                                string portStr = localAddress.Split(':').Last();
                                if (int.TryParse(portStr, out int port))
                                {
                                    ports.Add(port);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常，记录日志
                EventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            }

            return ports;
        }

        //服务启动时
        protected override void OnStart(string[] args)
        {
            //启动命名管道服务器
            StartPipeServer();
        }

        //服务停止时
        protected override void OnStop()
        {
            //停止所有代理的进程
            foreach (var process in managedProcesses.Values)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                    process.Dispose();
                }
            }
            managedProcesses.Clear();

            //停止命名管道服务器
            cancellationTokenSource.Cancel();
        }
    }
}
