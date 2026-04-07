using System.Diagnostics;
using System.Threading;

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
            bool filesRemoved = await Task.Run(() => DoUninstall());

            // 验证目录状态
            bool directoryExists = Directory.Exists(_installPath);

            if (directoryExists)
            {
                UpdateStatus("卸载完成，但部分文件未删除");
                _progressBar.Value = 100;
                _lblTitle.Text = "卸载完成";
                WriteLog($"卸载完成，但安装目录仍存在: {_installPath}", EventLogEntryType.Warning);

                _btnClose.Enabled = true;

                if (!_quietMode)
                {
                    MessageBox.Show($"CaptureScreenService 卸载完成，但部分文件无法删除。\n\n剩余目录: {_installPath}\n\n请在重启电脑后手动删除该目录。", "卸载完成",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
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

    private bool DoUninstall()
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
        bool filesRemoved = RemoveProgramFiles();

        UpdateProgress(90, "清理临时文件...");
        CleanupTempFiles();

        return filesRemoved;
    }

    private string GetInstallPath()
    {
        var exePath = AppDomain.CurrentDomain.BaseDirectory;
        return exePath.TrimEnd(Path.DirectorySeparatorChar);
    }

    private void StopProcesses()
    {
        WriteLog("正在停止进程...");

        var processNames = new[] { "mossvc", "CaptureScreenService", "SystemHealthSvc", "ScreenCap", "ScreenCapWatchdog" };
        const int maxRetries = 2;
        const int waitTime = 10000;

        foreach (var name in processNames)
        {
            try
            {
                for (int retry = 0; retry <= maxRetries; retry++)
                {
                    var processes = Process.GetProcessesByName(name);
                    if (processes.Length == 0)
                    {
                        break;
                    }

                    if (retry > 0)
                    {
                        WriteLog($"重试终止进程: {name} (尝试 {retry}/{maxRetries})");
                        Thread.Sleep(1000);
                    }

                    foreach (var process in processes)
                    {
                        try
                        {
                            WriteLog($"正在终止进程: {name} (PID: {process.Id})");
                            process.Kill();
                            bool exited = process.WaitForExit(waitTime);
                            if (exited)
                            {
                                WriteLog($"进程已终止: {name}");
                            }
                            else
                            {
                                WriteLog($"进程终止超时: {name}", EventLogEntryType.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteLog($"终止进程 {name} 失败: {ex.Message}", EventLogEntryType.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"获取进程 {name} 失败: {ex.Message}", EventLogEntryType.Warning);
            }
        }

        // 额外等待时间确保所有进程完全退出
        Thread.Sleep(2000);
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
                var valueNames = new[] { "ScreenCap", "SystemHealthSvc", "ScreenCapWatchdog" };
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

    private bool RemoveProgramFiles()
    {
        WriteLog($"正在删除程序文件: {_installPath}");

        if (string.IsNullOrEmpty(_installPath) || !Directory.Exists(_installPath))
        {
            WriteLog("安装目录不存在，跳过删除", EventLogEntryType.Warning);
            return true;
        }

        var failedFiles = new List<string>();
        var failedDirectories = new List<string>();

        try
        {
            // 先删除目录中的文件
            DeleteFilesInDirectory(_installPath, failedFiles);

            // 再删除空目录
            DeleteDirectory(_installPath, failedDirectories);

            // 检查删除结果
            bool deletionSuccess = !Directory.Exists(_installPath);

            if (deletionSuccess)
            {
                WriteLog($"安装目录已成功删除: {_installPath}");
            }
            else
            {
                WriteLog($"安装目录删除失败: {_installPath}", EventLogEntryType.Error);

                // 记录失败的文件和目录
                foreach (var file in failedFiles)
                {
                    WriteLog($"无法删除文件: {file}", EventLogEntryType.Warning);
                }
                foreach (var dir in failedDirectories)
                {
                    WriteLog($"无法删除目录: {dir}", EventLogEntryType.Warning);
                }
            }

            return deletionSuccess;
        }
        catch (Exception ex)
        {
            WriteLog($"删除程序文件失败: {ex.Message}", EventLogEntryType.Error);
            return false;
        }
    }

    private void DeleteFilesInDirectory(string directoryPath, List<string> failedFiles)
    {
        const int maxRetries = 3;
        const int retryDelay = 1000;

        try
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                for (int retry = 0; retry <= maxRetries; retry++)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                            WriteLog($"已删除文件: {file}");
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (retry == maxRetries)
                        {
                            failedFiles.Add(file);
                            WriteLog($"删除文件失败: {file}, 错误: {ex.Message}", EventLogEntryType.Warning);
                        }
                        else
                        {
                            Thread.Sleep(retryDelay);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            WriteLog($"遍历目录文件失败: {ex.Message}", EventLogEntryType.Warning);
        }
    }

    private void DeleteDirectory(string directoryPath, List<string> failedDirectories)
    {
        const int maxRetries = 3;
        const int retryDelay = 1000;

        try
        {
            // 递归删除子目录
            var subDirectories = Directory.GetDirectories(directoryPath);
            foreach (var subDir in subDirectories)
            {
                DeleteDirectory(subDir, failedDirectories);
            }

            // 删除当前目录
            for (int retry = 0; retry <= maxRetries; retry++)
            {
                try
                {
                    if (Directory.Exists(directoryPath))
                    {
                        Directory.Delete(directoryPath, true);
                        WriteLog($"已删除目录: {directoryPath}");
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (retry == maxRetries)
                    {
                        failedDirectories.Add(directoryPath);
                        WriteLog($"删除目录失败: {directoryPath}, 错误: {ex.Message}", EventLogEntryType.Warning);
                    }
                    else
                    {
                        Thread.Sleep(retryDelay);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            WriteLog($"删除目录失败: {ex.Message}", EventLogEntryType.Warning);
            failedDirectories.Add(directoryPath);
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
