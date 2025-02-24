using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace WindowsServiceAgent
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller serviceProcessInstaller1;
        private ServiceInstaller serviceInstaller1;

        public ProjectInstaller()
        {
            InitializeComponent();

            // 初始化安装程序组件
            serviceProcessInstaller1 = new ServiceProcessInstaller();
            serviceInstaller1 = new ServiceInstaller();

            // 设置默认值
            serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
            serviceInstaller1.ServiceName = "WindowsServiceAgent";
            serviceInstaller1.DisplayName = "WindowsServiceAgent Error!!!";
            serviceInstaller1.Description = "这个服务不应该存在，这是WindowsServiceAgent的默认创建服务，请在终端使用命令sc.exe delete WindowsServiceAgent删除";
            serviceInstaller1.StartType = ServiceStartMode.Automatic;

            // 添加到安装程序集合中
            Installers.AddRange(new Installer[] { serviceProcessInstaller1, serviceInstaller1 });
        }

        //重写安装方法
        public override void Install(IDictionary stateSaver)
        {
            // 获取安装时传递的参数
            string serviceName = Context.Parameters["ServiceName"];
            string displayName = Context.Parameters["DisplayName"];
            string description = Context.Parameters["Description"];
            string arguments = Context.Parameters["Arguments"];
            string startType = Context.Parameters["StartType"];
            string account = Context.Parameters["Account"];

            // 设置服务名称
            if (!string.IsNullOrEmpty(serviceName))
            {
                serviceInstaller1.ServiceName = serviceName;
            }

            // 设置显示名称
            if (!string.IsNullOrEmpty(displayName))
            {
                serviceInstaller1.DisplayName = displayName;
            }

            // 设置描述
            if (!string.IsNullOrEmpty(description))
            {
                serviceInstaller1.Description = description;
            }

            // 设置启动参数
            if (!string.IsNullOrEmpty(arguments))
            {
                string Path = $"\"{Context.Parameters["assemblypath"]}\" {arguments}";
                Context.Parameters["assemblypath"] = Path.ToString();
            }

            // 设置启动类型
            if (!string.IsNullOrEmpty(startType))
            {
                switch (startType.ToLower())
                {
                    case "automatic":
                        serviceInstaller1.StartType = ServiceStartMode.Automatic;
                        break;
                    case "manual":
                        serviceInstaller1.StartType = ServiceStartMode.Manual;
                        break;
                    case "disabled":
                        serviceInstaller1.StartType = ServiceStartMode.Disabled;
                        break;
                    default:
                        serviceInstaller1.StartType = ServiceStartMode.Manual;
                        break;
                }
            }

            // 设置服务运行账户
            if (!string.IsNullOrEmpty(account))
            {
                switch (account.ToLower())
                {
                    case "localsystem":
                        serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
                        break;
                    case "localservice":
                        serviceProcessInstaller1.Account = ServiceAccount.LocalService;
                        break;
                    case "networkservice":
                        serviceProcessInstaller1.Account = ServiceAccount.NetworkService;
                        break;
                    case "user":
                        serviceProcessInstaller1.Account = ServiceAccount.User;
                        // 如果是用户账户，还需要提供用户名和密码
                        string username = Context.Parameters["Username"];
                        string password = Context.Parameters["Password"];
                        serviceProcessInstaller1.Username = username;
                        serviceProcessInstaller1.Password = password;
                        break;
                    default:
                        serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
                        break;
                }
            }
            base.Install(stateSaver);
        }

        //重写卸载方法
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);
        }

    }

}
