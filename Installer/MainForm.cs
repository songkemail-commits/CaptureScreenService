using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Installer;

public partial class MainForm : Form
{
    private int _currentStep = 0;
    private readonly List<Panel> _pages = new();
    
    private RadioButton _radioLocal = null!;
    private RadioButton _radioEmail = null!;
    
    private TextBox _txtLocalPath = null!;
    private Button _btnBrowseLocal = null!;
    
    private RadioButton _radioQQ = null!;
    private RadioButton _radioNetEase = null!;
    private LinkLabel _linkQQAuth = null!;
    private LinkLabel _linkNetEaseAuth = null!;
    
    private TextBox _txtEmailAddress = null!;
    private TextBox _txtAuthCode = null!;
    
    private TextBox _txtInstallPath = null!;
    private Button _btnBrowseInstall = null!;
    
    private ProgressBar _progressBar = null!;
    private Label _lblStatus = null!;
    
    private string _installPath = @"C:\Program Files\CaptureScreenService";
    
    private readonly string _eventLogSource = "ScreenCapInstaller";
    private readonly string _eventLogName = "Application";

    public MainForm()
    {
        InitializeComponent();
        InitializeEventLog();
        SetupPages();
        ShowPage(0);
    }

    private void InitializeEventLog()
    {
        try
        {
            if (!EventLog.SourceExists(_eventLogSource))
            {
                EventLog.CreateEventSource(_eventLogSource, _eventLogName);
            }
        }
        catch
        {
        }
    }

    private void WriteLog(string message, EventLogEntryType type = EventLogEntryType.Information)
    {
        try
        {
            EventLog.WriteEntry(_eventLogSource, message, type);
        }
        catch
        {
        }
    }

    private void InitializeComponent()
    {
        this.Text = "CaptureScreenService 安装向导";
        this.Size = new Size(600, 450);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
    }

    private void SetupPages()
    {
        _pages.Add(CreateWelcomePage());
        _pages.Add(CreateInstallPathPage());
        _pages.Add(CreateStorageModePage());
        _pages.Add(CreateLocalConfigPage());
        _pages.Add(CreateEmailProviderPage());
        _pages.Add(CreateEmailConfigPage());
        _pages.Add(CreateInstallPage());
        _pages.Add(CreateFinishPage());

        foreach (var page in _pages)
        {
            this.Controls.Add(page);
        }
    }

    private Panel CreateWelcomePage()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Visible = false };
        
        var lblTitle = new Label
        {
            Text = "欢迎使用 CaptureScreenService 安装向导",
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 30)
        };
        
        var lblDesc = new Label
        {
            Text = "本程序将引导您完成 CaptureScreenService 的安装。\n\nCaptureScreenService 是一个 Windows 后台服务，用于定期截取屏幕。\n支持本地存储和邮箱发送两种模式。",
            Font = new Font("微软雅黑", 10),
            Location = new Point(20, 80),
            Size = new Size(540, 120)
        };
        
        var btnNext = CreateButton("下一步 >", 460, 350);
        btnNext.Click += (s, e) => NextPage();
        
        var btnCancel = CreateButton("取消", 370, 350);
        btnCancel.Click += (s, e) => Application.Exit();
        
        panel.Controls.AddRange(new Control[] { lblTitle, lblDesc, btnNext, btnCancel });
        return panel;
    }

    private Panel CreateInstallPathPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Visible = false };
        
        var lblTitle = new Label
        {
            Text = "选择安装路径",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20)
        };
        
        var lblDesc = new Label
        {
            Text = "请选择程序的安装目录：",
            Font = new Font("微软雅黑", 10),
            Location = new Point(20, 70),
            AutoSize = true
        };
        
        _txtInstallPath = new TextBox
        {
            Text = _installPath,
            Location = new Point(20, 100),
            Size = new Size(450, 30),
            Font = new Font("微软雅黑", 10)
        };
        
        _btnBrowseInstall = new Button
        {
            Text = "浏览...",
            Location = new Point(480, 98),
            Size = new Size(80, 32),
            Font = new Font("微软雅黑", 9)
        };
        _btnBrowseInstall.Click += (s, e) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择安装目录",
                SelectedPath = _txtInstallPath.Text
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _txtInstallPath.Text = dialog.SelectedPath;
                _installPath = dialog.SelectedPath;
            }
        };
        
        var btnBack = CreateButton("< 上一步", 280, 350);
        btnBack.Click += (s, e) => PrevPage();
        
        var btnNext = CreateButton("下一步 >", 460, 350);
        btnNext.Click += (s, e) => { _installPath = _txtInstallPath.Text; NextPage(); };
        
        var btnCancel = CreateButton("取消", 370, 350);
        btnCancel.Click += (s, e) => Application.Exit();
        
        panel.Controls.AddRange(new Control[] { lblTitle, lblDesc, _txtInstallPath, _btnBrowseInstall, btnBack, btnNext, btnCancel });
        return panel;
    }

    private Panel CreateStorageModePage()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Visible = false };
        
        var lblTitle = new Label
        {
            Text = "选择存储模式",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20)
        };
        
        var lblDesc = new Label
        {
            Text = "请选择截图的存储方式：",
            Font = new Font("微软雅黑", 10),
            Location = new Point(20, 70),
            AutoSize = true
        };
        
        _radioLocal = new RadioButton
        {
            Text = "本地存储 - 将截图保存到本地文件夹",
            Font = new Font("微软雅黑", 10),
            Location = new Point(30, 110),
            Size = new Size(500, 30),
            Checked = true
        };
        
        _radioEmail = new RadioButton
        {
            Text = "邮箱发送 - 将截图通过邮件发送",
            Font = new Font("微软雅黑", 10),
            Location = new Point(30, 150),
            Size = new Size(500, 30)
        };
        
        var btnBack = CreateButton("< 上一步", 280, 350);
        btnBack.Click += (s, e) => PrevPage();
        
        var btnNext = CreateButton("下一步 >", 460, 350);
        btnNext.Click += (s, e) =>
        {
            if (_radioLocal.Checked)
                GoToPage(3);
            else
                GoToPage(4);
        };
        
        var btnCancel = CreateButton("取消", 370, 350);
        btnCancel.Click += (s, e) => Application.Exit();
        
        panel.Controls.AddRange(new Control[] { lblTitle, lblDesc, _radioLocal, _radioEmail, btnBack, btnNext, btnCancel });
        return panel;
    }

    private Panel CreateLocalConfigPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Visible = false };
        
        var lblTitle = new Label
        {
            Text = "本地存储配置",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20)
        };
        
        var lblDesc = new Label
        {
            Text = "请选择截图保存的文件夹：",
            Font = new Font("微软雅黑", 10),
            Location = new Point(20, 70),
            AutoSize = true
        };
        
        _txtLocalPath = new TextBox
        {
            Text = @"C:\temp\TempPics",
            Location = new Point(20, 100),
            Size = new Size(450, 30),
            Font = new Font("微软雅黑", 10)
        };
        
        _btnBrowseLocal = new Button
        {
            Text = "浏览...",
            Location = new Point(480, 98),
            Size = new Size(80, 32),
            Font = new Font("微软雅黑", 9)
        };
        _btnBrowseLocal.Click += (s, e) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择截图保存目录",
                SelectedPath = _txtLocalPath.Text
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _txtLocalPath.Text = dialog.SelectedPath;
            }
        };
        
        var btnBack = CreateButton("< 上一步", 280, 350);
        btnBack.Click += (s, e) => GoToPage(2);
        
        var btnNext = CreateButton("安装 >", 460, 350);
        btnNext.Click += (s, e) => { GoToPage(6); };
        
        var btnCancel = CreateButton("取消", 370, 350);
        btnCancel.Click += (s, e) => Application.Exit();
        
        panel.Controls.AddRange(new Control[] { lblTitle, lblDesc, _txtLocalPath, _btnBrowseLocal, btnBack, btnNext, btnCancel });
        return panel;
    }

    private Panel CreateEmailProviderPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Visible = false };
        
        var lblTitle = new Label
        {
            Text = "选择邮箱提供商",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20)
        };
        
        var lblDesc = new Label
        {
            Text = "请选择您的邮箱提供商：",
            Font = new Font("微软雅黑", 10),
            Location = new Point(20, 70),
            AutoSize = true
        };
        
        _radioQQ = new RadioButton
        {
            Text = "QQ 邮箱 (smtp.qq.com:587)",
            Font = new Font("微软雅黑", 10),
            Location = new Point(30, 110),
            Size = new Size(500, 30),
            Checked = true
        };
        
        _radioNetEase = new RadioButton
        {
            Text = "网易邮箱 (smtp.163.com:465)",
            Font = new Font("微软雅黑", 10),
            Location = new Point(30, 150),
            Size = new Size(500, 30)
        };
        
        var btnBack = CreateButton("< 上一步", 280, 350);
        btnBack.Click += (s, e) => GoToPage(2);
        
        var btnNext = CreateButton("下一步 >", 460, 350);
        btnNext.Click += (s, e) => GoToPage(5);
        
        var btnCancel = CreateButton("取消", 370, 350);
        btnCancel.Click += (s, e) => Application.Exit();
        
        panel.Controls.AddRange(new Control[] { lblTitle, lblDesc, _radioQQ, _radioNetEase, btnBack, btnNext, btnCancel });
        return panel;
    }

    private Panel CreateEmailConfigPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Visible = false };
        
        var lblTitle = new Label
        {
            Text = "邮箱配置",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20)
        };
        
        var lblEmail = new Label
        {
            Text = "邮箱地址：",
            Font = new Font("微软雅黑", 11),
            Location = new Point(20, 80),
            AutoSize = true
        };
        
        _txtEmailAddress = new TextBox
        {
            Location = new Point(160, 77),
            Size = new Size(370, 35),
            Font = new Font("微软雅黑", 11),
            Height = 30
        };
        
        var lblAuth = new Label
        {
            Text = "授权码：",
            Font = new Font("微软雅黑", 11),
            Location = new Point(20, 140),
            AutoSize = true
        };
        
        _txtAuthCode = new TextBox
        {
            Location = new Point(160, 137),
            Size = new Size(370, 35),
            Font = new Font("微软雅黑", 11),
            PasswordChar = '*',
            Height = 30
        };
        
        var chkShowPassword = new CheckBox
        {
            Text = "显示授权码",
            Font = new Font("微软雅黑", 10),
            Location = new Point(160, 175),
            AutoSize = true
        };
        chkShowPassword.CheckedChanged += (s, e) => 
        {
            _txtAuthCode.PasswordChar = chkShowPassword.Checked ? '\0' : '*';
        };
        
        var lblHint = new Label
        {
            Text = "提示：授权码不是邮箱密码，请通过邮箱设置页面获取授权码",
            Font = new Font("微软雅黑", 9),
            ForeColor = Color.Gray,
            Location = new Point(20, 210),
            AutoSize = true
        };
        
        _linkQQAuth = new LinkLabel
        {
            Text = "获取 QQ 邮箱授权码",
            Font = new Font("微软雅黑", 9),
            ForeColor = Color.Blue,
            Location = new Point(160, 240),
            Size = new Size(200, 25),
            Visible = _radioQQ.Checked
        };
        _linkQQAuth.LinkClicked += (s, e) => OpenUrl("https://wx.mail.qq.com/list/readtemplate?name=app_intro.html#/agreement/authorizationCode");
        
        _linkNetEaseAuth = new LinkLabel
        {
            Text = "获取网易邮箱授权码",
            Font = new Font("微软雅黑", 9),
            ForeColor = Color.Blue,
            Location = new Point(160, 240),
            Size = new Size(200, 25),
            Visible = _radioNetEase.Checked
        };
        _linkNetEaseAuth.LinkClicked += (s, e) => OpenUrl("https://help.mail.163.com/faqDetail.do?code=d7a5dc8471cd0c0e8b4b8f4f8e49998b374173cfe9171305fa1ce630d7f67ac2a5feb28b66796d3b");
        
        var btnBack = CreateButton("< 上一步", 280, 350);
        btnBack.Click += (s, e) => GoToPage(4);
        
        var btnNext = CreateButton("安装 >", 460, 350);
        btnNext.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_txtEmailAddress.Text))
            {
                MessageBox.Show("请输入邮箱地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtAuthCode.Text))
            {
                MessageBox.Show("请输入授权码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GoToPage(6);
        };
        
        var btnCancel = CreateButton("取消", 370, 350);
        btnCancel.Click += (s, e) => Application.Exit();
        
        panel.Controls.AddRange(new Control[] { lblTitle, lblEmail, _txtEmailAddress, lblAuth, _txtAuthCode, chkShowPassword, lblHint, _linkQQAuth, _linkNetEaseAuth, btnBack, btnNext, btnCancel });
        return panel;
    }

    private Panel CreateInstallPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Visible = false };
        
        var lblTitle = new Label
        {
            Text = "正在安装",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20)
        };
        
        _progressBar = new ProgressBar
        {
            Location = new Point(20, 80),
            Size = new Size(540, 30),
            Style = ProgressBarStyle.Continuous
        };
        
        _lblStatus = new Label
        {
            Text = "准备安装...",
            Font = new Font("微软雅黑", 10),
            Location = new Point(20, 130),
            Size = new Size(540, 200)
        };
        
        panel.Controls.AddRange(new Control[] { lblTitle, _progressBar, _lblStatus });
        return panel;
    }

    private Panel CreateFinishPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Visible = false };
        
        var lblTitle = new Label
        {
            Text = "安装完成",
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 30)
        };
        
        var lblDesc = new Label
        {
            Text = "CaptureScreenService 已成功安装！\n\n服务已自动注册并启动，将按照配置定期执行截图任务。\n\n您可以通过 Windows 服务管理器管理此服务。",
            Font = new Font("微软雅黑", 10),
            Location = new Point(20, 80),
            Size = new Size(540, 150)
        };
        
        var btnFinish = CreateButton("完成", 460, 350);
        btnFinish.Click += (s, e) => Application.Exit();
        
        panel.Controls.AddRange(new Control[] { lblTitle, lblDesc, btnFinish });
        return panel;
    }

    private Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(80, 32),
            Font = new Font("微软雅黑", 9)
        };
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            _pages[i].Visible = (i == index);
        }
        _currentStep = index;
        
        if (index == 6)
        {
            DoInstall();
        }
    }

    private void NextPage() => ShowPage(_currentStep + 1);
    private void PrevPage() => ShowPage(_currentStep - 1);
    private void GoToPage(int index) => ShowPage(index);

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void DoInstall()
    {
        WriteLog("安装程序启动");
        Task.Run(() =>
        {
            try
            {
                UpdateStatus("停止现有服务...");
                WriteLog("停止现有服务...");
                StopService();
                _progressBar.Value = 5;

                UpdateStatus("终止相关进程...");
                WriteLog("终止相关进程...");
                KillRelatedProcesses();
                _progressBar.Value = 10;

                UpdateStatus("创建安装目录...");
                WriteLog($"创建安装目录: {_installPath}");
                Directory.CreateDirectory(_installPath);
                _progressBar.Value = 20;

                UpdateStatus("提取程序文件...");
                WriteLog("提取程序文件...");
                ExtractEmbeddedFiles();
                _progressBar.Value = 50;

                UpdateStatus("生成配置文件...");
                WriteLog("生成配置文件...");
                GenerateConfigFile();
                _progressBar.Value = 60;

                UpdateStatus("注册开机启动项...");
                WriteLog("注册开机启动项...");
                RegisterStartup();
                _progressBar.Value = 75;

                UpdateStatus("注册看门狗启动项...");
                WriteLog("注册看门狗启动项...");
                RegisterWatchdog();
                _progressBar.Value = 85;

                UpdateStatus("注册应用程序...");
                WriteLog("注册应用程序...");
                RegisterApplication();
                _progressBar.Value = 90;

                UpdateStatus("启动程序...");
                WriteLog("启动程序...");
                StartProgram();
                _progressBar.Value = 100;

                UpdateStatus("安装完成！");
                WriteLog("安装成功完成", EventLogEntryType.Information);
                this.Invoke(() =>
                {
                    MessageBox.Show("安装成功完成！程序将在开机时自动启动。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    GoToPage(7);
                });
            }
            catch (Exception ex)
            {
                WriteLog($"安装失败: {ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error);
                this.Invoke(() =>
                {
                    MessageBox.Show($"安装失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                });
            }
        });
    }

    private void RegisterApplication()
    {
        var uninstallKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\CaptureScreenService");
        
        uninstallKey.SetValue("DisplayName", "CaptureScreenService");
        uninstallKey.SetValue("DisplayVersion", "0.3");
        uninstallKey.SetValue("Publisher", "CaptureScreenService");
        uninstallKey.SetValue("InstallLocation", _installPath);
        uninstallKey.SetValue("DisplayIcon", Path.Combine(_installPath, "CaptureScreenService.exe"));
        var uninstallExePath = Path.Combine(_installPath, "uninstall.exe");
        uninstallKey.SetValue("UninstallString", $"\"{uninstallExePath}\"");
        uninstallKey.SetValue("QuietUninstallString", $"\"{uninstallExePath}\" /quiet");
        uninstallKey.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
        uninstallKey.SetValue("EstimatedSize", 1000, Microsoft.Win32.RegistryValueKind.DWord);
        uninstallKey.SetValue("NoModify", 1, Microsoft.Win32.RegistryValueKind.DWord);
        uninstallKey.SetValue("NoRepair", 1, Microsoft.Win32.RegistryValueKind.DWord);
        uninstallKey.Close();
        
        WriteLog($"注册表项已创建: UninstallString = {uninstallExePath}");
    }

    private void ExtractEmbeddedFiles()
    {
        var assembly = typeof(MainForm).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("Installer.ServiceFiles."))
            .ToList();

        foreach (var resourceName in resourceNames)
        {
            var fileName = resourceName.Substring("Installer.ServiceFiles.".Length);
            var destPath = Path.Combine(_installPath, fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var fileStream = File.Create(destPath);
            stream!.CopyTo(fileStream);
        }
    }

    private void UpdateStatus(string message)
    {
        this.Invoke(() => _lblStatus.Text = message);
    }

    private void GenerateConfigFile()
    {
        var storageMode = _radioLocal.Checked ? "Local" : "Email";
        var localPath = _txtLocalPath.Text;
        
        var emailProvider = _radioQQ.Checked ? "QQ" : "NetEase";
        var smtpServer = _radioQQ.Checked ? "smtp.qq.com" : "smtp.163.com";
        var smtpPort = _radioQQ.Checked ? 587 : 465;
        var emailAddress = _txtEmailAddress.Text;
        var encryptedAuthCode = EncryptAuthCode(_txtAuthCode.Text);

        var config = new
        {
            Logging = new
            {
                LogLevel = new { Default = "Information" },
                EventLog = new
                {
                    SourceName = "ScreenCapSvc",
                    LogName = "Application",
                    LogLevel = new
                    {
                        Microsoft = "Warning",
                        Microsoft_Hosting_Lifetime = "Information",
                        CaptureScreenService = "Information"
                    }
                }
            },
            AppConfig = new
            {
                StorageMode = storageMode,
                CaptureIntervalMinutes = 5,
                Local = new { SavePath = localPath },
                Email = new
                {
                    Provider = emailProvider,
                    SmtpServer = smtpServer,
                    SmtpPort = smtpPort,
                    EmailAddress = emailAddress,
                    EncryptedAuthCode = encryptedAuthCode
                }
            },
            Guardian = new { RestartDelayMinutes = 5 }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        json = json.Replace("Microsoft_Hosting_Lifetime", "Microsoft.Hosting.Lifetime");
        File.WriteAllText(Path.Combine(_installPath, "appsettings.json"), json);
    }

    private static string EncryptAuthCode(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var entropy = new byte[] { 0x53, 0x63, 0x72, 0x65, 0x65, 0x6E, 0x43, 0x61, 0x70 };
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, entropy, DataProtectionScope.LocalMachine);
        return Convert.ToBase64String(encryptedBytes);
    }

    private void StopService()
    {
        RunCommand("sc.exe", "stop CaptureScreenService");
        System.Threading.Thread.Sleep(2000);
        RunCommand("sc.exe", "delete CaptureScreenService");
        System.Threading.Thread.Sleep(1000);
    }

    private void KillRelatedProcesses()
    {
        try
        {
            var processes = Process.GetProcessesByName("CaptureScreenService");
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch { }
            }
            var watchdogProcesses = Process.GetProcessesByName("ScreenCapWatchdog");
            foreach (var process in watchdogProcesses)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch { }
            }
        }
        catch { }
    }

    private void RegisterStartup()
    {
        var exePath = Path.Combine(_installPath, "CaptureScreenService.exe");
        var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        runKey?.SetValue("ScreenCap", exePath);
        runKey?.Close();
    }

    private void RegisterWatchdog()
    {
        var watchdogPath = Path.Combine(_installPath, "ScreenCapWatchdog.exe");
        var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        runKey?.SetValue("ScreenCapWatchdog", watchdogPath);
        runKey?.Close();
    }

    private void StartProgram()
    {
        var watchdogPath = Path.Combine(_installPath, "ScreenCapWatchdog.exe");
        Process.Start(new ProcessStartInfo
        {
            FileName = watchdogPath,
            UseShellExecute = true
        });
    }

    private static bool RunCommand(string fileName, string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas"
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
