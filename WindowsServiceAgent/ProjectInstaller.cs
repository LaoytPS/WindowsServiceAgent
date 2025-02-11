using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
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

            // 添加到安装程序集合中
            Installers.AddRange(new Installer[] { serviceProcessInstaller1, serviceInstaller1 });

            // 设置默认值（稍后通过代码动态更改）
            serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
            serviceInstaller1.ServiceName = "WindowsServiceAgent";
            serviceInstaller1.DisplayName = "Windows Service Agent";
            serviceInstaller1.Description = "Agent service to host applications.";
            serviceInstaller1.StartType = ServiceStartMode.Automatic;
        }

        public override void Install(IDictionary stateSaver)
        {
            // 在调用 base.Install(stateSaver) 之前，处理传递的参数
            string serviceName = Context.Parameters["ServiceName"];
            string displayName = Context.Parameters["DisplayName"];
            string description = Context.Parameters["Description"];
            string startType = Context.Parameters["StartType"];
            string account = Context.Parameters["Account"];
            string username = Context.Parameters["Username"];
            string password = Context.Parameters["Password"];

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

            // 设置运行账户
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
                        serviceProcessInstaller1.Username = username;
                        serviceProcessInstaller1.Password = password;
                        break;
                    default:
                        serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
                        break;
                }
            }

            // 调用基类的 Install 方法
            base.Install(stateSaver);
        }

        public override void Uninstall(IDictionary savedState)
        {
            // 可以在此处理卸载时的逻辑（如果需要）
            base.Uninstall(savedState);
        }
    }

}
