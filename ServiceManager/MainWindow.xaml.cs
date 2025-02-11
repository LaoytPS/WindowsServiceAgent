using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Collections;
using System.Diagnostics;
using System.IO.Pipes;
using Newtonsoft.Json;
using System.IO;
using System.Collections.ObjectModel;
using ServiceLibrary;
using Microsoft.Win32;

namespace ServiceManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary



    public partial class MainWindow : Window
    {
        // 可观察合集存储服务列表
        private ObservableCollection<ServiceInfo> serviceList = new ObservableCollection<ServiceInfo>();

        public MainWindow()
        {
            InitializeComponent();
            // 绑定 DataGrid 的 ItemsSource
            servicesDataGrid.ItemsSource = serviceList;

            // 调用加载服务列表
            LoadServices();

            // 调用定时器
            StartStatusRefreshTimer();
        }

        // 安装服务
        private void InstallService(ServiceConfig config)
        {
            try
            {
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string serviceExePath = System.IO.Path.Combine(currentDirectory, "WindowsServiceAgent.exe");

                if (!System.IO.File.Exists(serviceExePath))
                {
                    MessageBox.Show("WindowsServiceAgent.exe 未找到，请确保它与 ServiceManager 在同一目录。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 构建安装参数
                var installArgs = new List<string>
                {
                    "/i",
                    $"/ServiceName={config.ServiceName}",
                    $"/DisplayName={config.DisplayName}",
                    $"/Description={config.Description}",
                    $"/StartType={config.StartType}",
                    $"/Account={config.Account}"
                };

                if (config.Account.Equals("User", StringComparison.OrdinalIgnoreCase))
                {
                    installArgs.Add($"/Username={config.Username}");
                    installArgs.Add($"/Password={config.Password}");
                }

                installArgs.Add(serviceExePath);

                // 调用 InstallUtil.exe
                string installUtilPath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory() + "InstallUtil.exe";

                Process installProcess = new Process();
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
                }
                else
                {
                    MessageBox.Show("服务安装失败。\n输出信息：" + output + "\n错误信息：" + error, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("服务安装过程中发生错误：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        // 卸载服务
        private void UninstallService(string serviceName)
        {
            try
            {
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string serviceExePath = System.IO.Path.Combine(currentDirectory, "WindowsServiceAgent.exe");

                if (!System.IO.File.Exists(serviceExePath))
                {
                    MessageBox.Show("WindowsServiceAgent.exe 未找到，请确保它与 ServiceManager 在同一目录。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 构建卸载参数
                var uninstallArgs = new List<string>
        {
            "/u",
            $"/ServiceName={serviceName}",
            serviceExePath
        };

                // 调用 InstallUtil.exe
                string installUtilPath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory() + "InstallUtil.exe";

                Process uninstallProcess = new Process();
                uninstallProcess.StartInfo.FileName = installUtilPath;
                uninstallProcess.StartInfo.Arguments = string.Join(" ", uninstallArgs);
                uninstallProcess.StartInfo.UseShellExecute = false;
                uninstallProcess.StartInfo.RedirectStandardOutput = true;
                uninstallProcess.StartInfo.CreateNoWindow = true;

                uninstallProcess.Start();

                string output = uninstallProcess.StandardOutput.ReadToEnd();
                uninstallProcess.WaitForExit();

                if (uninstallProcess.ExitCode == 0)
                {
                    MessageBox.Show("服务卸载成功！", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("服务卸载失败。\n输出信息：" + output, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("服务卸载过程中发生错误：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //启动服务
        private void StartService(string serviceName)
        {
            try
            {
                ServiceController sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动服务时发生错误：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //停止服务
        private void StopService(string serviceName)
        {
            try
            {
                ServiceController sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("停止服务时发生错误：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //与服务通信
        private async Task<string> SendRequestToServiceAsync(ServiceRequest request)
        {
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "WindowsServiceAgent_Pipe", PipeDirection.InOut))
                {
                    await pipeClient.ConnectAsync(5000); // 超时时间 5 秒

                    using (StreamReader reader = new StreamReader(pipeClient))
                    using (StreamWriter writer = new StreamWriter(pipeClient) { AutoFlush = true })
                    {
                        string requestJson = JsonConvert.SerializeObject(request);

                        // 发送请求
                        await writer.WriteLineAsync(requestJson);

                        // 接收响应
                        string response = await reader.ReadLineAsync();

                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        //发送启动命令
        private async void StartManagedApplication(string serviceName)
        {
            var request = new ServiceRequest
            {
                Command = "Start",
                ServiceName = serviceName
            };

            string response = await SendRequestToServiceAsync(request);
            MessageBox.Show(response, "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        //发送停止命令
        private async void StopManagedApplication(string serviceName)
        {
            var request = new ServiceRequest
            {
                Command = "Stop",
                ServiceName = serviceName
            };

            string response = await SendRequestToServiceAsync(request);
            MessageBox.Show(response, "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        //获取服务状态
        private async Task<ServiceStatus> GetServiceStatusAsync(string serviceName)
        {
            var request = new ServiceRequest
            {
                Command = "Status",
                ServiceName = serviceName
            };

            string response = await SendRequestToServiceAsync(request);

            // 解析响应
            if (response.StartsWith("Error"))
            {
                return new ServiceStatus { ServiceName = serviceName, IsRunning = false };
            }
            else
            {
                var status = JsonConvert.DeserializeObject<ServiceStatus>(response);
                return status;
            }
        }

        //加载服务列表
        private void LoadServices()
        {
            var services = ServiceController.GetServices();

            foreach (var sc in services)
            {
                // 过滤，只添加由 WindowsServiceAgent 安装的服务
                if (sc.ServiceName.StartsWith("sa"))
                {
                    ServiceInfo serviceInfo = new ServiceInfo
                    {
                        ServiceName = sc.ServiceName,
                        DisplayName = sc.DisplayName,
                        Status = sc.Status.ToString(),
                        PID = 0,
                        Ports = ""
                    };
                    serviceList.Add(serviceInfo);
                }
            }
        }

        private DispatcherTimer statusRefreshTimer;

        //启动定时器
        private void StartStatusRefreshTimer()
        {
            statusRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            statusRefreshTimer.Tick += async (sender, e) =>
            {
                foreach (var service in serviceList)
                {
                    var status = await GetServiceStatusAsync(service.ServiceName);
                    service.Status = status.IsRunning ? "Running" : "Stopped";
                    service.PID = status.PID;
                    service.Ports = status.Ports != null ? string.Join(", ", status.Ports) : "";
                }
            };
            statusRefreshTimer.Start();
        }


        //控件事件
        //安装服务页
        //当启动账户类型选择了“用户”时
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

        //点击浏览按钮
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

        //点击安装服务按钮
        private void InstallServiceButton_Click(object sender, RoutedEventArgs e)
        {
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

            // 输入验证
            if (string.IsNullOrEmpty(config.ServiceName) || string.IsNullOrEmpty(config.ExecutablePath))
            {
                MessageBox.Show("服务名称和应用程序路径不能为空。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 调用安装服务的方法
            InstallService(config);

            // 安装完成后，刷新服务列表
            LoadServices();
        }

        //代理服务列表页
        //点击启动服务按钮
        private void StartServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ServiceInfo selectedService)
            {
                StartService(selectedService.ServiceName);
                // 通过命名管道通知服务启动被托管的应用程序
                StartManagedApplication(selectedService.ServiceName);
            }
        }

        //点击停止服务按钮
        private void StopServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ServiceInfo selectedService)
            {
                StopManagedApplication(selectedService.ServiceName);
                // 可根据需要停止服务本身
                // StopService(selectedService.ServiceName);
            }
        }

        //点击卸载服务按钮
        private void UninstallServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ServiceInfo selectedService)
            {
                UninstallService(selectedService.ServiceName);
                serviceList.Remove(selectedService);
            }
        }


    }
}
