using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
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
        private ServiceEvent serviceEvent;
        private readonly Logging log = new Logging();

        // 初始化事件
        public MainWindow()
        {
            InitializeComponent();

            // 实例化服务事件
            serviceEvent = new ServiceEvent();

            // 调用初始化日志
            log.InitializeLog();

            // 绑定 DataGrid 的 ItemsSource
            servicesDataGrid.ItemsSource = serviceList;

            // 窗口加载时调用
            Loaded += MainWindow_Loaded;

        }

        // 服务事件
        // 将加载服务列表变为此类内部方法，以便在控件中调用
        private async Task LoadServices()
        {
            // 调用服务事件
            serviceList = await serviceEvent.LoadServices();
            servicesDataGrid.ItemsSource = serviceList;
        }

        // 窗口加载时
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 调用加载服务
            serviceList = await serviceEvent.LoadServices();
            servicesDataGrid.ItemsSource = serviceList;
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
        private async void InstallServiceButton_Click(object sender, RoutedEventArgs e)
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
            serviceEvent.InstallService(config);

            //调用创建配置文件
            serviceEvent.CreateServiceConfig(config);

            // 安装完成后，刷新服务列表
            await LoadServices();
        }

        // 代理服务列表页
        // 点击启动服务按钮
        private async void StartServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ServiceInfo selectedService)
            {
                serviceEvent.StartService(selectedService.ServiceName);
            }
            await LoadServices();
        }

        // 点击停止服务按钮
        private async void StopServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ServiceInfo selectedService)
            {
                serviceEvent.StopService(selectedService.ServiceName);
            }
            await LoadServices();
        }

        // 点击卸载服务按钮
        private async void UninstallServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ServiceInfo selectedService)
            {
                serviceEvent.UninstallService(selectedService.ServiceName);
                serviceList.Remove(selectedService);
            }
            await LoadServices();
        }

    }
}
