using System.Diagnostics;

namespace Uninstaller;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        try
        {
            var quietMode = args.Contains("/quiet") || args.Contains("/s") || args.Contains("/S");
            Application.Run(new UninstallForm(quietMode));
        }
        catch (Exception ex)
        {
            try
            {
                if (!EventLog.SourceExists("ScreenCapUninstaller"))
                {
                    EventLog.CreateEventSource("ScreenCapUninstaller", "Application");
                }
                EventLog.WriteEntry("ScreenCapUninstaller", 
                    $"卸载程序启动失败: {ex.Message}\n{ex.StackTrace}", 
                    EventLogEntryType.Error);
            }
            catch { }
            
            MessageBox.Show($"卸载程序启动失败：{ex.Message}", "错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

public class UninstallForm : Form
{
    private readonly bool _quietMode;
    private ProgressBar _progressBar = null!;
    private Label _lblStatus = null!;
    private Label _lblTitle = null!;
    private Button _btnClose = null!;
    private string _installPath = "";
    private readonly string _eventLogSource = "ScreenCapUninstaller";
    private readonly string _eventLogName = "Application";

    public UninstallForm(bool quietMode = false)
    {
        _quietMode = quietMode;
        InitializeComponent();
        InitializeEventLog();
    }
    
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!_quietMode)
        {
            _ = StartUninstallAsync();
        }
    }

    private void InitializeComponent()
    {
        this.Text = "CaptureScreenService 卸载程序";
        this.Size = new Size(500, 280);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormClosing += OnFormClosing;

        _lblTitle = new Label
        {
            Text = "正在卸载 CaptureScreenService",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(20, 60),
            Size = new Size(440, 30),
            Style = ProgressBarStyle.Continuous
        };

        _lblStatus = new Label
        {
            Text = "准备卸载...",
            Font = new Font("微软雅黑", 10),
            Location = new Point(20, 100),
            Size = new Size(440, 80)
        };

        _btnClose = new Button
        {
            Text = "关闭",
            Location = new Point(380, 200),
            Size = new Size(80, 32),
            Font = new Font("微软雅黑", 9),
            Enabled = false
        };
        _btnClose.Click += (s, e) => Application.Exit();

        this.Controls.AddRange(new Control[] { _lblTitle, _progressBar, _lblStatus, _btnClose });
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

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_progressBar.Value < 100 && _progressBar.Value > 0)
        {
            e.Cancel = true;
        }
    }

    public async Task StartUninstallAsync()
    {
        WriteLog("卸载程序启动");
        
        try
        {
            await Task.Run(() => DoUninstall());
            
            UpdateStatus("卸载完成！");
            _progressBar.Value = 100;
            _lblTitle.Text = "卸载完成";
            WriteLog("卸载成功完成", EventLogEntryType.Information);
            
            _btnClose.Enabled = true;
            
            if (_quietMode)
            {
                Application.Exit();
            }
            else
            {
                MessageBox.Show("CaptureScreenService 已成功卸载！", "卸载完成", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"卸载失败：{ex.Message}");
            _lblTitle.Text = "卸载失败";
            WriteLog($"卸载失败: {ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error);
            
            _btnClose.Enabled = true;
            
            if (!_quietMode)
            {
                MessageBox.Show($"卸载过程中发生错误：\n{ex.Message}", "卸载失败", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void DoUninstall()
    {
        _installPath = GetInstallPath();
        WriteLog($"安装路径: {_installPath}");

        UpdateProgress(5, "停止相关进程...");
        StopProcesses();

        UpdateProgress(20, "删除启动项...");
        RemoveStartupEntries();

        UpdateProgress(40, "删除注册表项...");
        RemoveRegistryEntries();

        UpdateProgress(60, "删除程序文件...");
        RemoveProgramFiles();

        UpdateProgress(90, "清理临时文件...");
        CleanupTempFiles();
    }

    private string GetInstallPath()
    {
        var exePath = AppDomain.CurrentDomain.BaseDirectory;
        return exePath.TrimEnd(Path.DirectorySeparatorChar);
    }

    private void StopProcesses()
    {
        WriteLog("正在停止进程...");
        
        var processNames = new[] { "CaptureScreenService", "ScreenCapWatchdog" };
        
        foreach (var name in processNames)
        {
            try
            {
                var processes = Process.GetProcessesByName(name);
                foreach (var process in processes)
                {
                    try
                    {
                        WriteLog($"正在终止进程: {name} (PID: {process.Id})");
                        process.Kill();
                        process.WaitForExit(5000);
                        WriteLog($"进程已终止: {name}");
                    }
                    catch (Exception ex)
                    {
                        WriteLog($"终止进程 {name} 失败: {ex.Message}", EventLogEntryType.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"获取进程 {name} 失败: {ex.Message}", EventLogEntryType.Warning);
            }
        }
    }

    private void RemoveStartupEntries()
    {
        WriteLog("正在删除启动项...");
        
        try
        {
            var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            
            if (runKey != null)
            {
                var valueNames = new[] { "ScreenCap", "ScreenCapWatchdog" };
                foreach (var name in valueNames)
                {
                    try
                    {
                        var existing = runKey.GetValue(name);
                        if (existing != null)
                        {
                            runKey.DeleteValue(name, false);
                            WriteLog($"已删除启动项: {name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog($"删除启动项 {name} 失败: {ex.Message}", EventLogEntryType.Warning);
                    }
                }
                runKey.Close();
            }
        }
        catch (Exception ex)
        {
            WriteLog($"访问启动项注册表失败: {ex.Message}", EventLogEntryType.Warning);
        }
    }

    private void RemoveRegistryEntries()
    {
        WriteLog("正在删除注册表项...");
        
        try
        {
            var uninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\CaptureScreenService";
            var uninstallKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(uninstallKeyPath, true);
            
            if (uninstallKey != null)
            {
                uninstallKey.Close();
                Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(uninstallKeyPath, false);
                WriteLog("已删除卸载注册表项");
            }
            else
            {
                WriteLog("卸载注册表项不存在", EventLogEntryType.Warning);
            }
        }
        catch (Exception ex)
        {
            WriteLog($"删除注册表项失败: {ex.Message}", EventLogEntryType.Warning);
        }
    }

    private void RemoveProgramFiles()
    {
        WriteLog($"正在删除程序文件: {_installPath}");
        
        if (string.IsNullOrEmpty(_installPath) || !Directory.Exists(_installPath))
        {
            WriteLog("安装目录不存在，跳过删除", EventLogEntryType.Warning);
            return;
        }

        try
        {
            var parentDir = Directory.GetParent(_installPath)?.FullName;
            
            var tempScript = Path.Combine(Path.GetTempPath(), $"delete_folder_{Guid.NewGuid():N}.ps1");
            var scriptContent = $@"
Start-Sleep -Seconds 1
Remove-Item -Path '{_installPath}' -Recurse -Force -ErrorAction SilentlyContinue
";
            File.WriteAllText(tempScript, scriptContent);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{tempScript}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            Process.Start(startInfo);
            WriteLog("已启动后台删除任务");
            
            System.Threading.Thread.Sleep(2000);
            
            try
            {
                if (File.Exists(tempScript))
                    File.Delete(tempScript);
            }
            catch { }
        }
        catch (Exception ex)
        {
            WriteLog($"删除程序文件失败: {ex.Message}", EventLogEntryType.Error);
            throw;
        }
    }

    private void CleanupTempFiles()
    {
        WriteLog("正在清理临时文件...");
        
        try
        {
            var tempFiles = Directory.GetFiles(Path.GetTempPath(), "getadmin.vbs");
            foreach (var file in tempFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
            
            var tempScripts = Directory.GetFiles(Path.GetTempPath(), "uninstall_script.ps1");
            foreach (var file in tempScripts)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }
        catch { }
    }

    private void UpdateProgress(int value, string status)
    {
        this.Invoke(() =>
        {
            _progressBar.Value = value;
            _lblStatus.Text = status;
        });
        WriteLog(status);
    }

    private void UpdateStatus(string status)
    {
        this.Invoke(() => _lblStatus.Text = status);
    }
}
