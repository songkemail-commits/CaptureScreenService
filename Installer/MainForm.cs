using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;

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

    private string _installPath = @"C:\Program Files\mossvc";

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
        this.Size = new Size(700, 480);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
    }

    private void SetupPages()
    {
        _pages.Add(CreateWelcomePage());
        _pages.Add(CreateEulaPage());
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
            Size = new Size(640, 120)
        };

        var btnNext = CreateButton("下一步 >", 560, 380);
        btnNext.Click += (s, e) => NextPage();

        var btnCancel = CreateButton("取消", 470, 380);
        btnCancel.Click += (s, e) => Application.Exit();

        panel.Controls.AddRange(new Control[] { lblTitle, lblDesc, btnNext, btnCancel });
        return panel;
    }

    private Panel CreateEulaPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Visible = false };

        var lblTitle = new Label
        {
            Text = "最终用户许可协议 (EULA)",
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 30)
        };

        var lblDesc = new Label
        {
            Text = "请仔细阅读以下许可协议。只有同意本协议的条款，才能继续安装。",
            Font = new Font("微软雅黑", 10),
            Location = new Point(20, 80),
            Size = new Size(640, 40)
        };

        // 创建滚动文本框显示EULA内容
        var txtEula = new TextBox
        {
            Text = ReadEulaContent(),
            Font = new Font("Consolas", 9),
            Location = new Point(20, 130),
            Size = new Size(640, 180),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            WordWrap = true
        };

        // 创建同意复选框
        var chkAgree = new CheckBox
        {
            Text = "我已阅读并同意本许可协议的所有条款",
            Font = new Font("微软雅黑", 10),
            Location = new Point(20, 320),
            AutoSize = true
        };

        // 创建按钮
        var btnBack = CreateButton("< 上一步", 380, 380);
        btnBack.Click += (s, e) => PrevPage();

        var btnNext = CreateButton("下一步 >", 560, 380);
        btnNext.Enabled = false; // 初始禁用
        btnNext.Click += (s, e) => NextPage();

        var btnCancel = CreateButton("取消", 470, 380);
        btnCancel.Click += (s, e) => Application.Exit();

        // 绑定复选框事件
        chkAgree.CheckedChanged += (s, e) =>
        {
            btnNext.Enabled = chkAgree.Checked;
        };

        panel.Controls.AddRange(new Control[] { lblTitle, lblDesc, txtEula, chkAgree, btnBack, btnNext, btnCancel });
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
            Size = new Size(550, 30),
            Font = new Font("微软雅黑", 10)
        };

        _btnBrowseInstall = new Button
        {
            Text = "浏览...",
            Location = new Point(580, 98),
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

        var btnBack = CreateButton("< 上一步", 380, 380);
        btnBack.Click += (s, e) => PrevPage();

        var btnNext = CreateButton("下一步 >", 560, 380);
        btnNext.Click += (s, e) => { _installPath = _txtInstallPath.Text; NextPage(); };

        var btnCancel = CreateButton("取消", 470, 380);
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

        var btnBack = CreateButton("< 上一步", 380, 380);
        btnBack.Click += (s, e) => PrevPage();

        var btnNext = CreateButton("下一步 >", 560, 380);
        btnNext.Click += (s, e) =>
        {
            if (_radioLocal.Checked)
                GoToPage(4);
            else
                GoToPage(5);
        };

        var btnCancel = CreateButton("取消", 470, 380);
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

        var btnBack = CreateButton("< 上一步", 380, 380);
        btnBack.Click += (s, e) => GoToPage(2);

        var btnNext = CreateButton("安装 >", 560, 380);
        btnNext.Click += (s, e) => { GoToPage(7); };

        var btnCancel = CreateButton("取消", 470, 380);
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

        var btnBack = CreateButton("< 上一步", 380, 380);
        btnBack.Click += (s, e) => GoToPage(2);

        var btnNext = CreateButton("下一步 >", 560, 380);
        btnNext.Click += (s, e) => GoToPage(6);

        var btnCancel = CreateButton("取消", 470, 380);
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

        var btnBack = CreateButton("< 上一步", 380, 380);
        btnBack.Click += (s, e) => GoToPage(4);

        var btnNext = CreateButton("安装 >", 560, 380);
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
            GoToPage(7);
        };

        var btnCancel = CreateButton("取消", 470, 380);
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

        var btnFinish = CreateButton("完成", 560, 380);
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

    private string ReadEulaContent()
    {
        try
        {
            // 尝试读取EULA.txt文件
            string eulaPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? "", "EULA.txt");

            // 如果在当前目录找不到，尝试从上级目录读取
            if (!File.Exists(eulaPath))
            {
                eulaPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.ExecutablePath) ?? "") ?? "", "CaptureScreenService", "EULA.txt");
            }

            if (File.Exists(eulaPath))
            {
                return File.ReadAllText(eulaPath, Encoding.UTF8);
            }

            // 如果文件不存在，返回默认的EULA内容
            return "电脑使用监控工具 (Computer Usage Monitoring Tool)\n" +
                   "最终用户许可协议 (End User License Agreement)\n\n" +
                   "版本：0.3\n" +
                   "开发者：songkemail-commits\n\n" +
                   "1. 协议接受\n\n" +
                   "安装并使用本软件，即表示您同意本协议的所有条款和条件。如果您不同意本协议，请不要安装或使用本软件。\n\n" +
                   "2. 软件介绍\n\n" +
                   "电脑使用监控工具是一款开源的屏幕监控软件，主要功能包括：\n" +
                   "- 定期截取计算机屏幕内容\n" +
                   "- 支持本地存储截图文件\n" +
                   "- 支持通过邮件发送截图\n" +
                   "- 提供服务监控功能确保稳定运行\n\n" +
                   "3. 许可证条款\n\n" +
                   "本软件采用 MIT 开源协议。您可以：\n" +
                   "- 自由使用本软件用于任何目的\n" +
                   "- 自由修改本软件的源代码\n" +
                   "- 自由分发本软件的原始或修改版本\n\n" +
                   "您只需在分发时包含原有的版权声明和本协议文本。\n\n" +
                   "4. 开源免责条款\n\n" +
                   "4.1 二开责任\n" +
                   "基于本软件修改、扩展或衍生的任何版本：\n" +
                   "- 原开发者不对修改版本的功能负责\n" +
                   "- 原开发者不对修改版本的安全性负责\n" +
                   "- 原开发者不对修改版本造成的任何损失负责\n" +
                   "- 修改版本必须明确标注为非原始版本\n\n" +
                   "4.2 责任限制\n" +
                   "在法律允许的最大范围内：\n" +
                   "- 本软件按\"原样\"提供，不提供任何明示或暗示的担保\n" +
                   "- 原开发者不对使用本软件造成的直接、间接、偶然或必然损失负责\n" +
                   "- 原开发者不对因软件缺陷导致的任何损害负责\n" +
                   "- 原开发者不对第三方系统的兼容性问题负责\n\n" +
                   "5. 使用限制\n\n" +
                   "您不得：\n" +
                   "- 使用本软件进行非法监控\n" +
                   "- 使用本软件侵犯他人隐私\n" +
                   "- 使用本软件违反任何法律法规\n" +
                   "- 使用本软件提供商业监控服务\n" +
                   "- 移除软件中的版权声明和许可证信息\n\n" +
                   "6. 隐私政策\n\n" +
                   "6.1 数据收集\n" +
                   "本软件仅收集以下信息：\n" +
                   "- 屏幕截图内容（根据您配置的频率）\n" +
                   "- 您提供的邮件配置信息\n" +
                   "- 必要的系统信息（用于服务运行）\n\n" +
                   "6.2 数据存储\n" +
                   "- 本地存储模式：截图保存在您指定的本地路径\n" +
                   "- 邮件发送模式：截图发送到您配置的邮箱地址\n" +
                   "- 配置信息：保存在软件安装目录的配置文件中\n\n" +
                   "6.3 数据安全\n" +
                   "- 邮件认证信息采用加密存储\n" +
                   "- 您负责保护本地存储的截图数据\n" +
                   "- 卸载软件时不会自动删除您的数据文件\n" +
                   "- 建议定期清理不需要的截图数据\n\n" +
                   "6.4 数据使用\n" +
                   "- 截图数据仅用于您的个人监控目的\n" +
                   "- 原开发者不会收集、存储或访问您的截图数据\n" +
                   "- 邮件发送仅用于将截图发送到您自己的邮箱\n\n" +
                   "7. 协议修改\n\n" +
                   "原开发者保留修改本协议的权利。修改后的协议将随软件更新一起发布。\n\n" +
                   "8. 终止\n\n" +
                   "您可以随时停止使用本软件。如您违反本协议，原开发者有权要求您停止使用本软件。\n\n" +
                   "9. 适用法律\n\n" +
                   "本协议适用中华人民共和国法律。如发生争议，双方应友好协商解决；协商不成的，任何一方均有权向有管辖权的人民法院提起诉讼。\n\n" +
                   "10. 完整协议\n\n" +
                   "本协议构成您与原开发者之间关于本软件的完整协议，取代之前的所有口头或书面协议。\n\n" +
                   "----------------------------------------------------------------------\n\n" +
                   "MIT License\n\n" +
                   "Copyright (c) 2026 songkemail-commits\n\n" +
                   "Permission is hereby granted, free of charge, to any person obtaining a copy\n" +
                   "of this software and associated documentation files (the \"Software\"), to deal\n" +
                   "in the Software without restriction, including without limitation the rights\n" +
                   "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell\n" +
                   "copies of the Software, and to permit persons to whom the Software is\n" +
                   "furnished to do so, subject to the following conditions:\n\n" +
                   "The above copyright notice and this permission notice shall be included in all\n" +
                   "copies or substantial portions of the Software.\n\n" +
                   "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\n" +
                   "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\n" +
                   "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\n" +
                   "AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\n" +
                   "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\n" +
                   "OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\n" +
                   "SOFTWARE.";
        }
        catch
        {
            // 如果读取失败，返回默认内容
            return "无法读取EULA文件，请联系开发者。";
        }
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            _pages[i].Visible = (i == index);
        }
        _currentStep = index;

        if (index == 7)
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
                    GoToPage(8);
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

        uninstallKey.SetValue("DisplayName", "mossvc");
        uninstallKey.SetValue("DisplayVersion", "0.3");
        uninstallKey.SetValue("Publisher", "mossvc");
        uninstallKey.SetValue("InstallLocation", _installPath);
        uninstallKey.SetValue("DisplayIcon", Path.Combine(_installPath, "mossvc.exe"));
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

        var entropy = GenerateEntropy();
        var encryptedAuthCode = EncryptAuthCode(_txtAuthCode.Text, entropy);

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
                },
                Security = new { Entropy = entropy }
            },
            Guardian = new { RestartDelayMinutes = 5 }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        json = json.Replace("Microsoft_Hosting_Lifetime", "Microsoft.Hosting.Lifetime");
        File.WriteAllText(Path.Combine(_installPath, "appsettings.json"), json);
    }

    private static string GenerateEntropy()
    {
        byte[] entropy = new byte[16];
        RandomNumberGenerator.Fill(entropy);
        return Convert.ToBase64String(entropy);
    }

    private static string EncryptAuthCode(string plainText, string base64Entropy)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var entropy = Convert.FromBase64String(base64Entropy);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, entropy, DataProtectionScope.LocalMachine);
        return Convert.ToBase64String(encryptedBytes);
    }

    private void StopService()
    {
        RunCommand("sc.exe", "stop mossvc");
        System.Threading.Thread.Sleep(2000);
        RunCommand("sc.exe", "delete mossvc");
        System.Threading.Thread.Sleep(1000);
        // Also stop and delete old service name for compatibility
        RunCommand("sc.exe", "stop CaptureScreenService");
        System.Threading.Thread.Sleep(1000);
        RunCommand("sc.exe", "delete CaptureScreenService");
        System.Threading.Thread.Sleep(1000);
    }

    private void KillRelatedProcesses()
    {
        try
        {
            var processes = Process.GetProcessesByName("mossvc");
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch { }
            }
            var oldProcesses = Process.GetProcessesByName("CaptureScreenService");
            foreach (var process in oldProcesses)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch { }
            }
            var watchdogProcesses = Process.GetProcessesByName("SystemHealthSvc");
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
        var exePath = Path.Combine(_installPath, "mossvc.exe");
        var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        runKey?.SetValue("ScreenCap", exePath);
        runKey?.Close();
    }

    private void RegisterWatchdog()
    {
        var watchdogPath = Path.Combine(_installPath, "SystemHealthSvc.exe");
        var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        runKey?.SetValue("SystemHealthSvc", watchdogPath);
        runKey?.Close();
    }

    private void StartProgram()
    {
        var watchdogPath = Path.Combine(_installPath, "SystemHealthSvc.exe");
        Process.Start(new ProcessStartInfo
        {
            FileName = watchdogPath,
            UseShellExecute = true,
            WorkingDirectory = _installPath
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
