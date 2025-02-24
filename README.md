<h1 align="center">
  <br>
  <img src="https://github.com/LaoytPS/WindowsServiceAgent/blob/main/Github/Image/WSA1.png?raw=true" width="256"/>
  <br>
  WindowsServiceAgent
  <br>
</h1>

<div align="center">
  <a href="https://github.com/LaoytPS/WindowsServiceAgent/blob/main/Github/Readme/README_zh-CN.md">中文说明</a> |
  <a href="https://github.com/LaoytPS/WindowsServiceAgent/releases">Download</a> |
  <a href="https://github.com/LaoytPS/WindowsServiceAgent/blob/main/Github/Changelog/Changelog.md">Changelog</a>
</div>

# 🪪 Overview

**WindowsServiceAgent** (hereinafter referred to as **WSA**) is software designed to help programs that cannot communicate with the Windows Service Control Manager to be registered as Windows services.  
**WindowsServiceAgentManager** (hereinafter referred to as **WSAM**) is software designed to help **WSA** quickly install services and provide basic management.

> [!IMPORTANT]
> I am a novice beginner and not very familiar with various open-source licenses. This project is currently copyrighted by me, and in the future, I will choose a suitable open-source license for the project.

# 📘 Table of Contents
- [💡 How It Works](#💡-how-it-works)
  - [Running](#running)
  - [Configuration File](#configuration-file)
- [📖 Instructions](#📖-instructions)
  - [Using WSAM (Recommended)](#using-wsam-recommended)
    - [Installing the Service](#installing-the-service)
    - [Managing Services](#managing-services)
  - [Using Without WSAM (Not Recommended)](#using-without-wsam-not-recommended)
    - [Installing the Service with InstallUtil.exe](#installing-the-service-with-installutilexe)
    - [Installing the Service with sc.exe](#installing-the-service-with-scexe)
    - [Creating and Editing the Configuration File](#creating-and-editing-the-configuration-file)
    - [Starting the Service](#starting-the-service)
- [🤔 Frequently Asked Questions](#🤔-frequently-asked-questions)
  - [Why Do I Need This Software](#why-do-i-need-this-software)
  - [Why Not Use Software Like Winsw or NSSM](#why-not-use-software-like-winsw-or-nssm)
- [🥵 Known Issues](#🥵-known-issues)
  - [WSA](#wsa)
  - [WSAM](#wsam)

# 💡 How It Works <a id="💡-how-it-works"></a>

**WSA** is a Windows service binary program that runs based on configuration files and startup parameters, only applicable to higher versions of Windows platforms.

> [!NOTE]
> Higher versions of Windows platforms refer to **Windows Server 2012 R2** and above.

## Running

**WSA** cannot be directly opened and used in Windows; it needs to be installed using **sc.exe** or **InstallUtil.exe** before it can run as a service, and each proxy service will start a new **WindowsServiceAgent.exe** instance. When the configuration file changes, **WSA** will automatically restart to reload the configuration file.

> [!WARNING]
> If you need the service to run stably, please set the configuration file attribute to **read-only**

## Configuration File

The configuration file is one of the important conditions for **WSA** to run independently. The configuration file contains the following parameters:
- **ExecutablePath** (required): Path of the application being proxied
- **Arguments** (required): Arguments for the application being proxied
- **WorkingDirectory** (required): Working directory of the application being proxied

> [!NOTE]
> Unless otherwise needed, `WorkingDirectory` should be set to the directory where the application is located

# 📖 Instructions <a id="📖-instructions"></a>

## Using WSAM (Recommended) <a id="using-wsam-recommended"></a>

In most cases, using **WSAM** to install services is your preferred method.

### Installing the Service

In general, there are only three steps to installation: open the program, fill in the form, click install.

- First, open **WSAM**  
- Then, on the installation page, fill in the **parameters** according to your situation  
- Finally, click the install button to complete **installing the service**  

**WSAM** will automatically install the service and create the configuration file

> [!IMPORTANT]
> When using **WSAM**, please ensure it is in the same directory as **WSA**  
> Services installed by **WSAM** will have the `wsa_` prefix added to the service name

### Managing Services

**WSAM** provides simple service management, including start, stop, and uninstall.

- First, open **WSAM**  
- Then, click **Proxy Service List**
- Finally, click the corresponding button for the service you want to operate on in the list

> [!NOTE]
> Please make sure to run this application as an administrator  
> Currently, **WSAM** only provides management for services created by this program

## Using Without WSAM (Not Recommended) <a id="using-without-wsam-not-recommended"></a>

This is a **not recommended** method for installing services unless absolutely necessary, such as when there is no GUI interface

### Installing the Service with InstallUtil.exe <a id="installing-the-service-with-installutilexe"></a>

Taking the installation on a 64-bit Windows 11 23H2 system as an example:

- First, determine the location of the `InstallUtil.exe` application on your system  
  For example: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe`

- Then, open the system's **PowerShell** or **CMD** and enter the installation command to complete the installation
  
  - PowerShell command example  
    ```
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /i /ServiceName=你的服务名称 /DisplayName="你的显示名称" /Description="你的服务描述" /StartType=你的启动类型 /Account=你的启动账户 /Arguments="-c 你的配置文件名" 你的服务程序路径
    ```

  - CMD command example  
    ```
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /i "/ServiceName=你的服务名称" "/DisplayName=你的显示名称" "/Description=你的服务描述" "/StartType=你的启动类型" "/Account=你的启动账户" "/Arguments=-c 你的配置文件名" "你的服务程序路径"
    ```   
> [!NOTE]
> Please replace the Chinese in the command with the parameters you need before executing the command  
> If using a user as the startup account, you can add the `/Username` and `/Password` parameters

### Installing the Service with sc.exe <a id="installing-the-service-with-scexe"></a>

Again, using a 64-bit Windows 11 23H2 system as an example:

- Open the system's **PowerShell** or **CMD** and enter the installation command to complete the installation

  - PowerShell command example 
    ```
    sc.exe create 你的服务名称 binPath= "你的服务程序路径 -c 你的配置文件名" DisplayName= "你的服务显示名称" start= 你的启动类型 obj= 你的启动账户
    ```  
  - CMD command example
    ```
    sc create 你的服务名称 binPath= "你的服务程序路径 -c 你的配置文件名" DisplayName= "你的服务显示名称" start= 你的启动类型 obj= 你的启动账户
    ```  

> [!NOTE]
> To add a service description, please enter the following command after entering the above command:  
> `sc.exe description yourservicename "YourServiceDescription"`

### Creating and Editing the Configuration File

- First, create a `ServiceConfigs` folder in the same directory as **WSA**, and create a `configname`.json text file with the same name as in the previous command within the folder.
- Then, in `configname`.json, write the [configuration file](#configuration-file) according to the JSON format
  - JSON example
    ```json
    {
      "ExecutablePath": "被代理的应用程序路径",
      "Arguments": "被代理的应用程序参数",
      "WorkingDirectory": "被代理的应用程序运行路径"
    }
    ```  

### Starting the Service

- Use your preferred method to start the service. The following is an example command
  - PowerShell/CMD example
    ```
    net start YourServiceName
    ```
        
> [!NOTE]
> Please make sure to run this application as an administrator

# 🤔 Frequently Asked Questions <a id="🤔-frequently-asked-questions"></a>

## Why Do I Need This Software

Because some software does not support communication with the Windows Service Control Manager, such as PHP, Nginx, etc. This software was created to solve that problem. 💕

## Why Not Use Software Like Winsw or NSSM <a id="why-not-use-software-like-winsw-or-nssm"></a>

Great question! When I was writing this README file, I also just learned about those software options. 😭 Perhaps the only advantage of this software is that it allows running multiple instances of services with executable files that occupy extremely minimal storage space.

# 🥵 Known Issues <a id="🥵-known-issues"></a>

## WSA

No known issues

## WSAM

- On the proxy service list page, there is a row with three non-functional buttons
- In the proxy service list, querying the PID and port can only retrieve the process ID and port number of a single proxy instance; if multiple instances exist, it will still only display one
