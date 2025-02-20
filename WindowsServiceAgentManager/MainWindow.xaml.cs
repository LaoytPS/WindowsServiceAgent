using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WindowsServiceAgentManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>

    public partial class MainWindow : Window
    {
        // 初始化变量
        // 可观察合集存储服务列表
        private ObservableCollection<ServiceInfo> serviceList = new ObservableCollection<ServiceInfo>();
        readonly Logging log = new Logging();

        public MainWindow()
        {
            InitializeComponent();
            // 绑定 DataGrid 的 ItemsSource
            servicesDataGrid.ItemsSource = serviceList;

            // 调用初始化日志
            log.InitializeLog();

            // 调用加载服务列表
            LoadServices();

        }

        // 服务事件
        // 安装服务
        private void InstallService(ServiceConfig config)
        {
            log.Log($"正在执行安装{config.ServiceName}服务中", EventLogType.信息);
            try
            {
                string serviceExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WindowsServiceAgent.exe");
                // 调试输出
                // log.Log($"获取当前路径为{currentDirectory}", EventLogType.调试);
                // log.Log($"获取程序路径为{serviceExePath}", EventLogType.调试);

                // 确保当前路径下存在 WindowsServiceAgent.exe
                if (!File.Exists(serviceExePath))
                {
                    MessageBox.Show("WindowsServiceAgent.exe 未找到，请确保它与 ServiceManager 在同一目录。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    log.Log("安装服务失败，原因：“WindowsServiceAgent.exe 未找到，请确保它与 ServiceManager 在同一目录。”", EventLogType.错误);
                    return;
                }
        
                // 构建安装参数
                var installArgs = new List<string>
                {
                    "/i",
                    $"/ServiceName=wsa_{config.ServiceName}",
                    $"/DisplayName={config.DisplayName}",
                    $"/Description={config.Description}",
                    $"/StartType={config.StartType}",
                    $"/Account={config.Account}",
                    $"/Arguments=\"-sn {config.ServiceName}\""
                };
                // 调试输出
                // log.Log($"确认服务名称{config.ServiceName}", EventLogType.调试);

                if (config.Account.Equals("User", StringComparison.OrdinalIgnoreCase))
                {
                    installArgs.Add($"/Username={config.Username}");
                    installArgs.Add($"/Password={config.Password}");
                }
        
                installArgs.Add(serviceExePath);
                // 调试输出
                // log.Log($"已添加最后的路径参数{serviceExePath}", EventLogType.调试);

                // 调用 InstallUtil.exe
                string installUtilPath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory() + "InstallUtil.exe";
                // 调试输出
                // log.Log($"获取InstallUtil路径为{installUtilPath}", EventLogType.调试);

                using (Process installProcess = new Process())
                {
                    installProcess.StartInfo.FileName = installUtilPath;
                    installProcess.StartInfo.Arguments = string.Join(" ", installArgs);
                    installProcess.StartInfo.UseShellExecute = false;
                    installProcess.StartInfo.RedirectStandardOutput = true;
                    installProcess.StartInfo.RedirectStandardError = true;
                    installProcess.StartInfo.CreateNoWindow = true;
        
                    installProcess.Start();
        
                    string output = installProcess.StandardOutput.ReadToEnd();
                    string error = installProcess.StandardError.ReadToEnd();
                    installProcess.WaitForExit();
        
                    if (installProcess.ExitCode == 0)
                    {
                        MessageBox.Show("服务安装成功！", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                        log.Log("服务安装成功！", EventLogType.信息);
                    }
                    else
                    {
                        MessageBox.Show("服务安装失败。\n输出信息：" + output + "\n错误信息：" + error, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        log.Log($"服务安装失败！\n输出信息\n{output}，\n错误信息\n{error}", EventLogType.错误);
                    }
                }

                // 调试输出
                // foreach (var i in installArgs)
                // {
                //     log.Log($"确认命令列表{i}", EventLogType.调试);
                // }
            }
            catch (Exception ex)
            {
                MessageBox.Show("服务安装过程中发生错误：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                log.Log($"服务安装过程中发生错误：{ex.Message}", EventLogType.错误);
            }
        
        }

        // 卸载服务
        private void UninstallService(string serviceName)
        {
            log.Log($"正在执行卸载 {serviceName} 服务中", EventLogType.信息);
            try
            {
                // 构建 sc.exe 路径
                string scExePath = Path.Combine(Environment.SystemDirectory, "sc.exe");

                // 检查 sc.exe 是否存在
                if (!File.Exists(scExePath))
                {
                    MessageBox.Show("sc.exe 未找到，请确保它存在于系统目录中。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    log.Log("卸载服务失败，原因：“sc.exe 未找到，请确保它存在于系统目录中。”", EventLogType.错误);
                    return;
                }

                // 构建 sc.exe 命令参数
                string uninstallArgs = $"delete \"{serviceName}\"";

                // 调用 sc.exe
                using (Process uninstallProcess = new Process())
                {
                    uninstallProcess.StartInfo.FileName = scExePath;
                    uninstallProcess.StartInfo.Arguments = uninstallArgs;
                    uninstallProcess.StartInfo.UseShellExecute = false;
                    uninstallProcess.StartInfo.RedirectStandardOutput = true;
                    uninstallProcess.StartInfo.RedirectStandardError = true;
                    uninstallProcess.StartInfo.CreateNoWindow = true;

                    uninstallProcess.Start();

                    string output = uninstallProcess.StandardOutput.ReadToEnd();
                    string error = uninstallProcess.StandardError.ReadToEnd();
                    uninstallProcess.WaitForExit();

                    if (uninstallProcess.ExitCode == 0)
                    {
                        MessageBox.Show("服务卸载命令已执行，请注意服务删除可能需要一段时间完成。", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                        log.Log($"服务卸载命令已执行。\n输出信息：\n{output}", EventLogType.信息);
                    }
                    else
                    {
                        MessageBox.Show($"服务卸载失败。\n错误信息：\n{error}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        log.Log($"服务卸载失败。\n输出信息：\n{output}\n错误信息：\n{error}", EventLogType.错误);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("服务卸载过程中发生错误：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                log.Log($"服务卸载过程中发生错误：{ex.Message}", EventLogType.错误);
            }
        }

        // 启动服务
        private void StartService(string serviceName)
        {
            log.Log($"正在尝试启动{serviceName}服务", EventLogType.信息);
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    }
                    else
                    {
                        MessageBox.Show("服务已经在运行。", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("启动服务时发生错误：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                log.Log($"启动服务时发生错误：{ex.Message}", EventLogType.错误);
            }
        }

        // 停止服务
        private void StopService(string serviceName)
        {
            log.Log($"正在尝试停止{serviceName}服务", EventLogType.信息);
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }
                    else
                    {
                        MessageBox.Show("服务已经停止。", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("停止服务时发生错误：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                log.Log($"停止服务时发生错误：{ex.Message}", EventLogType.错误);
            }
        }

        // 加载服务列表
        private async void LoadServices()
        {
            log.Log("正在加载服务列表", EventLogType.信息);
            serviceList.Clear();

            var services = ServiceController.GetServices();

            foreach (var sc in services)
            {
                if (sc.ServiceName.StartsWith("wsa_"))
                {
                    ServiceInfo serviceInfo = new ServiceInfo
                    {
                        ServiceName = sc.ServiceName,
                        DisplayName = sc.DisplayName,
                        Status = sc.Status.ToString()
                    };

                    // 异步获取 PID 和 Ports
                    await Task.Run(() =>
                    {
                        serviceInfo.PID = GetAgentProcessId(serviceInfo.ServiceName);
                        serviceInfo.Ports = serviceInfo.PID > 0 ? GetAgentProcessPorts(serviceInfo.PID) : "未运行";
                    });

                    serviceList.Add(serviceInfo);
                }
            }
        }

        // 获取服务对应的代理进程 PID
        private int? GetAgentProcessId(string serviceName)
        {
            try
            {
                // 获取服务对应的配置文件
                string configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceConfigs");
                string configPath = Path.Combine(configDirectory, $"{serviceName.Replace("wsa_", "")}.json"); // 移除前缀

                if (!File.Exists(configPath))
                {
                    log.Log($"未找到服务 {serviceName} 的配置文件 {configPath}", EventLogType.警告);
                    return null;
                }

                // 读取配置文件
                string json = File.ReadAllText(configPath);
                ServiceConfig config = JsonConvert.DeserializeObject<ServiceConfig>(json);

                if (config == null || string.IsNullOrEmpty(config.ExecutablePath))
                {
                    log.Log($"服务 {serviceName} 的配置文件内容无效", EventLogType.警告);
                    return null;
                }

                // 获取所有正在运行的进程
                Process[] processes = Process.GetProcesses();

                // 标准化路径
                string targetPath = Path.GetFullPath(config.ExecutablePath).ToLowerInvariant();

                foreach (Process process in processes)
                {
                    try
                    {
                        string processPath = process.MainModule.FileName;
                        if (!string.IsNullOrEmpty(processPath))
                        {
                            processPath = Path.GetFullPath(processPath).ToLowerInvariant();

                            // 比较可执行文件路径
                            if (processPath == targetPath)
                            {
                                return process.Id;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 忽略无法访问的进程（权限不足）
                        continue;
                    }
                }

                log.Log($"未找到服务 {serviceName} 对应的正在运行的进程", EventLogType.警告);
                return null;
            }
            catch (Exception ex)
            {
                log.Log($"获取服务 {serviceName} 的代理进程 PID 时发生错误：{ex.Message}", EventLogType.错误);
                return null;
            }
        }

        // 获取服务对应的代理进程监听端口
        private string GetAgentProcessPorts(int? pid)
        {
            try
            {
                List<int> portList = new List<int>();

                // 调用 netstat 命令，获取所有 TCP 连接和监听端口
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "netstat.exe";
                psi.Arguments = "-ano";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;

                using (Process netstatProcess = Process.Start(psi))
                {
                    string output = netstatProcess.StandardOutput.ReadToEnd();
                    netstatProcess.WaitForExit();

                    // 解析 netstat 输出
                    string[] lines = output.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        if (line.StartsWith("  TCP") || line.StartsWith("  UDP"))
                        {
                            string[] tokens = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                            if (tokens.Length >= 5)
                            {
                                string localAddress = tokens[1];
                                string pidStr = tokens[tokens.Length - 1];

                                if (int.TryParse(pidStr, out int processId) && processId == pid)
                                {
                                    // 提取端口号
                                    string portStr = localAddress.Split(':').Last();
                                    if (int.TryParse(portStr, out int port))
                                    {
                                        if (!portList.Contains(port))
                                        {
                                            portList.Add(port);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (portList.Count > 0)
                {
                    return string.Join(", ", portList);
                }
                else
                {
                    return "未占用端口";
                }
            }
            catch (Exception ex)
            {
                log.Log($"获取进程 {pid} 的端口信息时发生错误：{ex.Message}", EventLogType.错误);
                return "获取端口失败";
            }
        }

        // 创建服务配置文件
        private void CreateServiceConfig(ServiceConfig config)
        {
            string configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceConfigs");
            // 确保日志目录存在
            if (!Directory.Exists(configDirectory))
            {
                log.Log("未能找到ServiceConfigs文件夹，已创建新的ServiceConfigs文件夹", EventLogType.警告);
                Directory.CreateDirectory(configDirectory);
            }
            string configPath = Path.Combine(configDirectory, $"{config.ServiceName}.json");
            if (!File.Exists(configPath))
            {
                log.Log($"正在尝试创建{config.ServiceName}.json配置文件", EventLogType.信息);
                string json = JsonConvert.SerializeObject(config);
                File.WriteAllText(configPath, json);
            }
            else
            {
                log.Log($"注意：当前文件夹下已有{config.ServiceName}的配置文件，将覆盖创建新的{config.ServiceName}.json文件", EventLogType.警告);
                string json = JsonConvert.SerializeObject(config);
                File.WriteAllText(configPath, json);
            }
        }

        // 控件事件
        // 安装服务页
        // 当启动账户类型选择了“用户”时
        private void CmbAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbAccount.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Content.ToString().Equals("User", StringComparison.OrdinalIgnoreCase))
                {
                    userAccountPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    userAccountPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        // 点击浏览按钮
        private void BrowseExecutablePath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                txtExecutablePath.Text = openFileDialog.FileName;
                txtWorkingDirectory.Text = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
            }
        }

        // 点击安装服务按钮
        private void InstallServiceButton_Click(object sender, RoutedEventArgs e)
        {
            log.Log("开始获取输入配置信息", EventLogType.信息);
            // 获取用户输入的配置信息
            ServiceConfig config = new ServiceConfig
            {
                ServiceName = txtServiceName.Text.Trim(),
                DisplayName = txtDisplayName.Text.Trim(),
                Description = txtDescription.Text.Trim(),
                ExecutablePath = txtExecutablePath.Text.Trim(),
                Arguments = txtArguments.Text.Trim(),
                WorkingDirectory = txtWorkingDirectory.Text.Trim(),
                StartType = ((ComboBoxItem)cmbStartType.SelectedItem)?.Content.ToString(),
                Account = ((ComboBoxItem)cmbAccount.SelectedItem)?.Content.ToString(),
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Password
            };
            log.Log($"当前保存的服务名称为{config.ServiceName}，当前输入框的服务名称为{txtServiceName}", EventLogType.信息);

            // 输入验证
            if (string.IsNullOrEmpty(config.ServiceName) || string.IsNullOrEmpty(config.ExecutablePath))
            {
                MessageBox.Show("服务名称和应用程序路径不能为空。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            log.Log("开始调用InstallService", EventLogType.信息);
            // 调用安装服务
            InstallService(config);

            //调用创建配置文件
            CreateServiceConfig(config);

            // 安装完成后，刷新服务列表
            LoadServices();
        }

        // 代理服务列表页
        // 点击启动服务按钮
        private void StartServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ServiceInfo selectedService)
            {
                StartService(selectedService.ServiceName);
            }
            LoadServices();
        }

        // 点击停止服务按钮
        private void StopServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ServiceInfo selectedService)
            {
                StopService(selectedService.ServiceName);
            }
            LoadServices();
        }

        // 点击卸载服务按钮
        private void UninstallServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ServiceInfo selectedService)
            {
                UninstallService(selectedService.ServiceName);
                serviceList.Remove(selectedService);
            }
            LoadServices();
        }

    }
}
