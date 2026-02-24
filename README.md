# CaptureScreenService

## 项目概述

CaptureScreenService 是一个功能强大的 Windows 后台服务，用于定期自动截取屏幕内容并支持多种存储方式。该服务采用守护进程架构，具有自动故障恢复能力，确保长时间稳定运行。

![Screen Capture Service](https://trae-api-cn.mchost.guru/api/ide/v1/text_to_image?prompt=Windows%20screen%20capture%20service%20icon%20with%20monitor%20and%20camera%20symbol%2C%20professional%20blue%20color%2C%20clean%20design&image_size=square)

## 功能特点

- 📸 **自动屏幕截图**：定期截取主屏幕内容
- 💾 **本地存储**：将截图保存到指定文件夹
- 📧 **邮箱发送**：支持 QQ 邮箱和网易邮箱发送截图
- 🔒 **安全加密**：使用 Windows DPAPI 加密存储邮箱授权码
- 🛡️ **守护进程**：自动监控和恢复工作进程
- 📊 **详细日志**：记录到 Windows 事件查看器
- 🚀 **自包含部署**：无需安装 .NET 运行时

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

## 系统要求

- **操作系统**: Windows 10/11 或 Windows Server 2016+
- **架构**: x64
- **运行时**: 无需安装（自包含部署，包含 .NET 9.0 运行时）

## 安装方式

### 方式一：使用安装程序（推荐）

1. 下载最新的安装程序 `install.exe`
2. 双击运行安装向导
3. 按照向导步骤完成配置
4. 安装完成后服务自动启动

### 方式二：手动安装

```powershell
# 编译项目
cd CaptureScreenService
dotnet publish -c Release

# 复制文件到目标目录
Copy-Item -Path "bin\Release\net9.0\win-x64\publish\*" -Destination "C:\Program Files\CaptureScreenService\" -Recurse -Force
```powershell
# 配置服务
cd "C:\Program Files\CaptureScreenService"
./CaptureScreenService.exe --configure

# 安装服务
./CaptureScreenService.exe --install
```

## 配置说明

配置文件位于 `C:\Program Files\CaptureScreenService\appsettings.json`

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
    },
    "Security": {
      "Entropy": "加密熵值"
    }
  },
  "Guardian": {
    "RestartDelayMinutes": 5
  }
}
```

## 命令行工具

| 命令 | 说明 |
|------|------|
| `--configure` | 交互式配置 |
| `--install` | 安装 Windows 服务 |
| `--uninstall` | 卸载 Windows 服务 |
| `--encrypt <code>` | 加密授权码 |
| `--decrypt <code>` | 解密授权码 |
| `--test` | 测试截图功能 |

## 邮箱授权码配置

### 获取授权码

1. **QQ 邮箱**：
   - 登录 QQ 邮箱
   - 进入 **设置** → **账户**
   - 开启 **SMTP服务**
   - 生成授权码

2. **网易邮箱**：
   - 登录网易邮箱
   - 进入 **设置** → **POP3/SMTP/IMAP**
   - 开启 **SMTP服务**
   - 生成授权码

### 加密授权码

```powershell
./CaptureScreenService.exe --encrypt "你的授权码"
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
./CaptureScreenService.exe --uninstall
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

## 安全注意事项

1. 服务以 Local System 账户运行，具有较高权限
2. 授权码使用 Windows DPAPI 加密存储，与当前用户绑定
3. 更换 Windows 用户后需要重新加密授权码
4. 请妥善保管配置文件
5. 建议定期更新授权码
6. 遵守相关法律法规，合理使用该工具

## 项目结构

```
CaptureScreenService0.3/
├── CaptureScreenService/       # 主服务项目
├── Installer/                  # 安装程序项目
├── Uninstaller/                # 卸载程序项目
├── Watchdog/                   # 守护进程项目
├── .gitignore                  # Git 忽略文件
├── cleanup.ps1                 # 清理脚本
├── DEPLOYMENT.md               # 部署文档
├── LICENSE.txt                 # 许可证文件
└── README.md                   # 项目说明
```

## 构建项目

### 构建主服务

```powershell
cd CaptureScreenService
dotnet build -c Release
```

### 构建安装程序

```powershell
cd Installer
./build.ps1
```

构建后的安装程序位于：`Installer\bin\Release\net9.0-windows\win-x64\publish\install.exe`

## 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE.txt](LICENSE.txt) 文件了解详情

## 贡献指南

欢迎贡献代码、报告问题或提出建议！请查看 [CONTRIBUTING.md](CONTRIBUTING.md) 文件了解如何参与项目。

## 代码行为准则

我们期望所有参与者能够遵循项目的行为准则，创造一个友好、包容的社区环境。请查看 [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) 文件了解详情。

## 版本信息

- **版本**: 0.3
- **目标框架**: .NET 9.0
- **最后更新**: 2026年2月

## 免责声明

1. 本软件仅供教育和个人使用目的
2. 作者不对任何修改、改编或基于本软件的衍生作品负责
3. 作者不承担任何因使用或误用本软件而产生的责任
4. 用户应自行负责正确配置和保护软件
5. 确保遵守适用的法律法规
6. 确保软件的使用符合伦理和法律要求

---

**注意**: 使用本软件时请遵守相关法律法规，尊重他人隐私。
