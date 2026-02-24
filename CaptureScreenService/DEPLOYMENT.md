# CaptureScreenService 部署和使用文档

## 项目概述

CaptureScreenService 是一个 Windows 后台服务，用于定期截取屏幕。支持两种存储方式：
- **本地存储**：将截图保存到本地文件夹
- **邮箱发送**：将截图通过邮件发送（支持 QQ 邮箱、网易邮箱）

该服务采用守护进程架构，具有自动故障恢复能力。

## 系统架构

```
┌─────────────────────────────────────────────────────────┐
│                   Windows 服务管理器                      │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│              GuardianService (守护进程)                   │
│  - 监控 Worker 进程状态                                   │
│  - 进程异常退出后等待 5 分钟自动重启                        │
└─────────────────────────────────────────────────────────┘
                           │
                           │ 启动/监控
                           ▼
┌─────────────────────────────────────────────────────────┐
│              Worker (工作进程)                            │
│  - 每 5 分钟执行一次屏幕截图                               │
│  - 本地存储模式：保存 PNG 到指定目录                        │
│  - 邮箱发送模式：转换为 JPG (≤100KB) 并发送邮件             │
└─────────────────────────────────────────────────────────┘
```

## 环境要求

- **操作系统**: Windows 10/11 或 Windows Server 2016+
- **运行时**: 无需安装（自包含部署，包含 .NET 9.0 运行时）
- **架构**: x64

## 安装方式

### 方式一：使用安装程序（推荐）

#### 1. 运行安装程序

双击 `install.exe` 启动安装向导。

#### 2. 安装向导步骤

| 步骤 | 说明 |
|------|------|
| **欢迎页面** | 介绍程序功能 |
| **安装路径** | 选择安装目录，默认 `C:\Program Files\CaptureScreenService` |
| **存储模式** | 选择本地存储或邮箱发送 |
| **本地配置** | 选择截图保存路径（本地存储模式） |
| **邮箱提供商** | 选择 QQ 邮箱或网易邮箱（邮箱发送模式） |
| **邮箱配置** | 输入邮箱地址和授权码 |
| **安装进度** | 显示安装进度 |
| **完成页面** | 安装成功提示 |

#### 3. 安装完成后

- 服务已自动注册为 Windows 服务
- 服务已自动启动
- 配置文件保存在安装目录下的 `appsettings.json`

### 方式二：手动安装

#### 1. 编译项目

```powershell
cd CaptureScreenService
dotnet publish -c Release
```

发布后的文件位于: `bin\Release\net9.0\win-x64\publish\`

#### 2. 复制文件到目标目录

```powershell
Copy-Item -Path "bin\Release\net9.0\win-x64\publish\*" -Destination "C:\Program Files\CaptureScreenService\" -Recurse -Force
```

#### 3. 配置服务

```powershell
cd "C:\Program Files\CaptureScreenService"
.\CaptureScreenService.exe --configure
```

#### 4. 安装服务

```powershell
.\CaptureScreenService.exe --install
```

## 构建安装程序

如需重新构建安装程序：

```powershell
cd Installer
.\build.bat
```

构建脚本会：
1. 编译主服务程序
2. 将服务文件嵌入到安装程序
3. 生成独立的 `install.exe`

输出位置：`Installer\bin\Release\net9.0-windows\win-x64\publish\install.exe`

## 命令行工具

| 命令 | 说明 |
|------|------|
| `--configure` | 交互式配置 |
| `--install` | 安装 Windows 服务 |
| `--uninstall` | 卸载 Windows 服务 |
| `--encrypt <code>` | 加密授权码 |
| `--decrypt <code>` | 解密授权码 |

## 配置说明

配置文件: `appsettings.json`

### 完整配置示例

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "EventLog": {
      "SourceName": "ScreenCapSvc",
      "LogName": "Application",
      "LogLevel": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "CaptureScreenService": "Information"
      }
    }
  },
  "AppConfig": {
    "StorageMode": "Email",
    "CaptureIntervalMinutes": 5,
    "Local": {
      "SavePath": "C:\\temp\\TempPics"
    },
    "Email": {
      "Provider": "QQ",
      "SmtpServer": "smtp.qq.com",
      "SmtpPort": 587,
      "EmailAddress": "your_email@qq.com",
      "EncryptedAuthCode": "加密后的授权码"
    }
  },
  "Guardian": {
    "RestartDelayMinutes": 5
  }
}
```

### 配置项说明

#### AppConfig 配置

| 配置项 | 说明 | 可选值 |
|--------|------|--------|
| `StorageMode` | 存储模式 | `Local`（本地存储）、`Email`（邮箱发送） |
| `CaptureIntervalMinutes` | 截图间隔（分钟） | 数字，默认 5 |

#### Local 配置（本地存储模式）

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `SavePath` | 截图保存路径 | `C:\temp\TempPics` |

#### Email 配置（邮箱发送模式）

| 配置项 | 说明 | QQ邮箱 | 网易邮箱 |
|--------|------|--------|----------|
| `Provider` | 邮箱提供商 | `QQ` | `NetEase` |
| `SmtpServer` | SMTP服务器 | `smtp.qq.com` | `smtp.163.com` |
| `SmtpPort` | SMTP端口 | `587` | `465` |
| `EmailAddress` | 邮箱地址 | 你的邮箱 | 你的邮箱 |
| `EncryptedAuthCode` | 加密后的授权码 | - | - |

## 获取邮箱授权码

### QQ 邮箱

1. 访问：https://wx.mail.qq.com/list/readtemplate?name=app_intro.html#/agreement/authorizationCode
2. 登录 QQ 邮箱
3. 进入 **设置** → **账户**
4. 找到 **POP3/IMAP/SMTP/Exchange/CardDAV/CalDAV服务**
5. 开启 **SMTP服务**
6. 生成授权码

### 网易邮箱

1. 访问：https://help.mail.163.com/faqDetail.do?code=d7a5dc8471cd0c0e8b4b8f4f8e49998b374173cfe9171305fa1ce630d7f67ac2a5feb28b66796d3b
2. 登录网易邮箱
3. 进入 **设置** → **POP3/SMTP/IMAP**
4. 开启 **SMTP服务**
5. 生成授权码

## 加密授权码

授权码需要加密后才能写入配置文件：

```powershell
.\CaptureScreenService.exe --encrypt "你的授权码"
```

加密后的授权码会保存到 `encrypt_output.txt` 文件中，将其复制到配置文件的 `EncryptedAuthCode` 字段。

## 管理服务

### 启动服务

```powershell
Start-Service -Name "CaptureScreenService"
```

### 停止服务

```powershell
Stop-Service -Name "CaptureScreenService"
```

### 查看服务状态

```powershell
Get-Service -Name "CaptureScreenService"
```

### 卸载服务

```powershell
.\CaptureScreenService.exe --uninstall
```

或手动卸载：

```powershell
Stop-Service -Name "CaptureScreenService"
sc.exe delete "CaptureScreenService"
```

## 查看日志

服务日志记录在 Windows 事件查看器中：

1. 打开 **事件查看器** (Event Viewer)
2. 导航至 **Windows 日志** → **应用程序**
3. 筛选源名称为 **ScreenCapSvc**

或使用 PowerShell:

```powershell
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='ScreenCapSvc'} -MaxEvents 50
```

## 故障排除

### 服务无法启动

1. 检查程序文件完整性
2. 查看事件日志中的错误信息
3. 确认配置文件格式正确

### 截图失败

1. 确认服务以具有桌面交互权限的账户运行
2. 检查存储目录权限
3. 查看事件日志了解详细错误

### 邮件发送失败

1. 检查网络连接
2. 确认 SMTP 服务器可访问
3. 检查授权码是否正确加密
4. 确认授权码未过期
5. 检查邮箱是否开启了 SMTP 服务

### 守护进程频繁重启工作进程

1. 查看事件日志了解工作进程退出原因
2. 检查系统资源（内存、CPU）
3. 确认没有防病毒软件干扰

## 文件结构

```
C:\Program Files\CaptureScreenService\
├── CaptureScreenService.exe    # 主程序
├── CaptureScreenService.dll    # 程序集
├── appsettings.json            # 配置文件
└── [其他依赖文件]

C:\temp\TempPics\               # 本地存储模式的截图目录
```

## 安全注意事项

1. 服务以 Local System 账户运行，具有较高权限
2. 授权码使用 Windows DPAPI 加密存储，与当前用户绑定
3. 更换 Windows 用户后需要重新加密授权码
4. 请妥善保管配置文件

## 版本信息

- 版本: 0.3
- 目标框架: .NET 9.0
- 最后更新: 2026年2月
