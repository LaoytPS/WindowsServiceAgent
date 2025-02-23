<h1 align="center">
  <br>
  <img src="https://github.com/LaoytPS/WindowsServiceAgent/blob/develop/Github/Image/WSA1.png?raw=true" width="256"/>
  <br>
  WindowsServiceAgent
  <br>
</h1>

<div align="center">
  <a href="https://github.com/LaoytPS/WindowsServiceAgent/blob/main/Github/Readme/README.md">English</a> |
  <a href="https://github.com/LaoytPS/WindowsServiceAgent/releases">下载软件</a> |
  <a href="https://github.com/LaoytPS/WindowsServiceAgent/blob/main/Github/Changelog/Changelog_zh-CN.md">更新说明</a>
</div>

# 🪪 概述
**WindowsServiceAgent**（以下简称**服务程序**）是一个旨在帮助无法与Windows服务控制管理器通信的程序注册为Windows服务的软件。  
**WindowsServiceAgentManager**（以下简称**管理程序**）是一个旨在帮助**服务程序**快速安装服务并且提供基础管理的软件。

> [!IMPORTANT]
> 本人是个菜鸟初学者，不太清楚各种开源协议，此项目目前暂为本人版权所有，日后会为项目选择合适的开源协议开源

# 📘 目录
- [💡如何运作](#-如何运作)
  - [运行](#运行)
  - [配置文件](#配置文件)
- [📖使用说明](#-使用说明)
  - [依靠管理程序使用（推荐）](#依靠管理程序使用（推荐）)
    - [安装服务](#安装服务)
    - [管理服务](#管理服务)
  - [不依靠管理程序使用（不推荐）](#不依靠管理程序使用（不推荐）)
    - [使用InstallUtil.exe安装服务](#使用InstallUtil.exe安装服务)
    - [使用sc.exe安装服务](#使用sc.exe安装服务)
    - [创建并编辑配置文件](#创建并编辑配置文件)
    - [启动服务](#启动服务)
- [🤔常见问题](#-常见问题)
  - [为什么需要这个软件](#为什么需要这个软件)
  - [为什么不用Winsw或NSSM等软件](#为什么不用Winsw或NSSM等软件)
- [🥵已知问题](#-已知问题)
  - [服务程序](#服务程序)
  - [管理程序](#管理程序)

# 💡 如何运作
**服务程序**是一个Windows服务的二进制程序，基于配置文件和启动参数运行，仅适用于较高版本的Windows平台上。

> [!NOTE]
> 较高版本的Windows平台，指**Windows Server 2012 R2**及以上

## 运行
**服务程序**无法直接在Windows中打开使用，需要使用**sc.exe**或**InstallUtil.exe**安装后才能运行服务，且每个代理服务都会启动一个新的**WindowsServiceAgent.exe**实例；当配置文件更变时，**服务程序**会自动重启以冷重载配置文件。
> [!WARNING]
> 如果需要服务稳定运行，请将配置文件属性设置为**只读**

## 配置文件
配置文件是**服务程序**就能独立运行的重要条件之一，其中配置文件包含以下参数：
- **ExecutablePath**（必要）：被代理的应用程序路径
- **Arguments**（必要）：被代理的应用程序参数
- **WorkingDirectory**（必要）：被代理的应用程序运行路径

> [!NOTE]
> 如无特殊需求，`WorkingDirectory`应填写为应用程序所在目录

# 📖 使用说明
## 依靠管理程序使用（推荐）<a id="依靠管理程序使用（推荐）"></a>
在大多数情况下，使用**管理程序**安装服务是您的首选方式。
### 安装服务
总的来说安装只有这三步：打开程序、填写表单、点击安装。  

- 首先，打开**管理程序**  
- 然后，在安装页面根据自己情况填写**参数**  
- 最后，点击安装按钮即可完成**安装服务**  

**管理程序**会自动安装服务并且创建配置文件
> [!IMPORTANT]
> 使用**管理程序**时，请确保和**服务程序**在同一目录  
> 由**管理程序**安装的服务，都会在服务名称中添加`wsa_`前缀

### 管理服务
**管理程序**提供了简单的服务管理，包括启动、停止和卸载。  

- 首先，打开**管理程序**  
- 然后，点击**代理服务列表**
- 最后，对列表中想要操作的服务点击对应按钮即可

> [!NOTE]
> 请确保以管理员身份运行这个应用程序
> 目前，**管理程序**只提供对由该程序创建的服务进行管理

## 不依靠管理程序使用（不推荐）<a id="不依靠管理程序使用（不推荐）"></a>
这是**不推荐**的安装服务方法，除非万不得已时，如：没有GUI界面
### 使用InstallUtil.exe安装服务 <a id="使用InstallUtil.exe安装服务"></a>
以64位Windows 11 23H2版本系统安装服务为例：

- 首先，确定自己系统InstallUtil.exe应用程序所在位置  
如`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe`

- 然后，打开系统的**PowerShell**或**CMD**并输入安装命令，即可完成安装
  
  - PowerShell命令示例  
    ```
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /i /ServiceName=你的服务名称 /DisplayName="你的显示名称" /Description="你的服务描述" /StartType=你的启动类型 /Account=你的启动账户 /Arguments="-c 你的配置文件名" 你的服务程序路径
    ```

  - CMD命令示例  
    ```
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /i "/ServiceName=你的服务名称" "/DisplayName=你的显示名称" "/Description=你的服务描述" "/StartType=你的启动类型" "/Account=你的启动账户" "/Arguments=-c 你的配置文件名" "你的服务程序路径"
    ```   
> [!NOTE]
> 请将命令中的中文换成你需要的参数，然后再执行命令
> 如果以用户作为启动账户，可添加/Username和/Password参数

### 使用sc.exe安装服务 <a id="使用sc.exe安装服务"></a>
还是以64位Windows 11 23H2版本系统安装服务为例：

- 打开系统的**PowerShell**或**CMD**并输入安装命令，即可完成安装

  - PowerShell命令示例 
    ```
    sc.exe create 你的服务名称 binPath= "你的服务程序路径 -c 你的配置文件名" DisplayName= "你的服务显示名称" start= 你的启动类型 obj= 你的启动账户
    ```  
  - CMD命令示例
    ```
    sc create 你的服务名称 binPath= "你的服务程序路径 -c 你的配置文件名" DisplayName= "你的服务显示名称" start= 你的启动类型 obj= 你的启动账户
    ```  

> [!NOTE]
> 如需添加服务描述，请在输入命令后再输入以下命令：  
> `sc.exe description 你的服务名称 "你的服务描述"`

### 创建并编辑配置文件
- 首先，在**服务程序**所在目录下创建`ServiceConfigs`文件夹，并且在文件夹中创建之前命令同名的`你的配置文件名`.json文本文件。
- 然后，在`你的配置文件名`.json中按照Json格式编写[配置文件](#配置文件)
  - Json示例
    ```json
    {
      "ExecutablePath": "被代理的应用程序路径",
      "Arguments": "被代理的应用程序参数",
      "WorkingDirectory": "被代理的应用程序运行路径"
    }
    ```  

### 启动服务
- 用你喜欢的方式启动服务，以下为示例命令
  - PowerShell/CMD示例
    ```
    net start 你的服务名称
    ```
    
> [!NOTE]
> 请确保以管理员身份运行这个应用程序

# 🤔 常见问题
## 为什么需要这个软件
因为有部分软件是不支持与Windows服务控制管理器通信的，如PHP、Nginx等，此软件就是为了解决这个问题而出现的。💕
## 为什么不用Winsw或NSSM等软件 <a id="为什么不用Winsw或NSSM等软件"></a>
非常好问题，当我正在写这个Readme文件的时候，我也是才知道有这些软件的😭。或许此软件的唯一优势，只有以存储空间占用极低的可执行文件进行多实例运行服务。

# 🥵 已知问题
## 服务程序
暂无已知问题
## 管理程序
- 在代理服务列表页中，会有一行拥有着3个无效的按钮
- 在代理服务列表中的pid和端口查询，只能查询单个代理实例的进程id和端口号，存在多个时也只会显示一个
