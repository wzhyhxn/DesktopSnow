using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading; // [新增] 用于 Mutex 单例检查
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32; // [新增] 用于操作注册表实现开机自启

// === 核心修复：明确指定使用 WPF 的 Application 和 MessageBox ===
// 这一步是为了解决 CS0104 “不明确的引用”错误
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

// 引入别名，防止和 WPF 的控件冲突
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace DesktopSnow
{
    public partial class MainWindow : Window
    {
        // 引用系统API用于监听按键
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        // === 单例检查变量 ===
        private static Mutex _mutex = null!;

        // === 托盘图标变量 ===
        private Forms.NotifyIcon _notifyIcon = null!;

        // === 雪花变量 ===
        // ============================================
        // 修复点 1：字段初始化警告
        // 使用 " = null!;" 告诉编译器：“放心，我会在后面初始化的，别报错”
        // ============================================
        private List<Snowflake> _snowflakes = new List<Snowflake>();
        private DispatcherTimer _animationTimer = null!;
        private DispatcherTimer _keyListenerTimer = null!;

        private Random _random = new Random();
        private double _screenWidth;
        private double _screenHeight;

        // === 配置项默认值 ===
        private int _configMode = 2;
        private bool _configStartupShow = true;
        private int _configStartupDuration = 5;
        private int _configKey = 120;

        private bool _isSnowing = false;
        private bool _wasKeyDown = false;

        public MainWindow()
        {
            // ============================================================
            // === [新增] 第一步：单例检查 (Single Instance Check) ===
            // 确保程序只能运行一个，防止重复打开
            // ============================================================
            bool isNewInstance;
            // "Global\DesktopSnowAppMutex" 是一个系统级的唯一身份证
            _mutex = new Mutex(true, "Global\\DesktopSnowAppMutex", out isNewInstance);

            if (!isNewInstance)
            {
                MessageBox.Show("桌面下雪程序已经在运行啦！\n请检查右下角托盘图标，或按 F12 退出。", "提示");
                Application.Current.Shutdown(); // 关掉当前这个多余的
                return; // 阻止后续代码执行
            }

            InitializeComponent();
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
            _screenWidth = this.Width;
            _screenHeight = this.Height;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. 初始化托盘图标
            InitTrayIcon();

            // 2. 加载配置和雪花
            LoadConfig();
            InitSnow();

            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(16);
            // 订阅事件
            _animationTimer.Tick += UpdateSnow;

            // 开机自动运行逻辑
            if (_configStartupShow)
            {
                StartSnow();
                var stopTimer = new DispatcherTimer();
                stopTimer.Interval = TimeSpan.FromSeconds(_configStartupDuration);
                stopTimer.Tick += (s, args) => { StopSnow(); stopTimer.Stop(); };
                stopTimer.Start();
            }

            // 开启按键监听
            _keyListenerTimer = new DispatcherTimer();
            _keyListenerTimer.Interval = TimeSpan.FromMilliseconds(50);
            _keyListenerTimer.Tick += CheckKeyListener;
            _keyListenerTimer.Start();
        }

        // ============================================
        // 托盘图标与开机自启逻辑
        // ============================================
        private void InitTrayIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Text = "桌面下雪 (Desktop Snow)";
            _notifyIcon.Visible = true;

            if (File.Exists("snow.ico"))
            {
                _notifyIcon.Icon = new Drawing.Icon("snow.ico");
            }
            else
            {
                _notifyIcon.Icon = Drawing.SystemIcons.Application;
            }

            // 创建右键菜单
            var contextMenu = new Forms.ContextMenuStrip();

            // [新增] 开机自启菜单
            var startupItem = new Forms.ToolStripMenuItem("开机自启");
            startupItem.Checked = IsStartupEnabled(); // 初始化勾选状态
            startupItem.Click += (s, e) =>
            {
                bool newState = !startupItem.Checked;
                SetStartup(newState);
                startupItem.Checked = newState;
            };
            contextMenu.Items.Add(startupItem);

            contextMenu.Items.Add(new Forms.ToolStripSeparator());

            // 切换下雪
            var toggleItem = contextMenu.Items.Add("切换下雪 (F9)");
            toggleItem.Click += (s, e) => ToggleSnow();

            // 退出
            var exitItem = contextMenu.Items.Add("退出程序");
            exitItem.Click += (s, e) => QuitApp();

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ToggleSnow();
        }

        // 设置注册表实现开机自启
        private void SetStartup(bool enable)
        {
            string appName = "DesktopSnow";
            string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!;

            if (enable) rk.SetValue(appName, appPath);
            else rk.DeleteValue(appName, false);
        }

        private bool IsStartupEnabled()
        {
            string appName = "DesktopSnow";
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)!;
            return rk.GetValue(appName) != null;
        }

        private void QuitApp()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void ToggleSnow()
        {
            if (_isSnowing) StopSnow();
            else StartSnow();
        }

        private void LoadConfig()
        {
            string configPath = "config.txt";
            if (File.Exists(configPath))
            {
                try
                {
                    var lines = File.ReadAllLines(configPath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length != 2) continue;
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        if (key == "Mode") int.TryParse(value, out _configMode);
                        if (key == "StartupShow") bool.TryParse(value, out _configStartupShow);
                        if (key == "StartupDuration") int.TryParse(value, out _configStartupDuration);
                        if (key == "Key") int.TryParse(value, out _configKey);
                    }
                }
                catch { }
            }
        }

        // ============================================
        // 修复点 2：参数类型不匹配警告
        // 把 object sender 改成 object? sender (允许为空)
        // ============================================
        private void CheckKeyListener(object? sender, EventArgs e)
        {
            // 123 代表 F12 键。按下 F12，程序彻底关闭。
            if ((GetAsyncKeyState(123) & 0x8000) != 0)
            {
                Application.Current.Shutdown(); // 彻底杀掉程序
                return;
            }

            bool isKeyDown = (GetAsyncKeyState(_configKey) & 0x8000) != 0;

            if (_configMode == 1) // 按住模式
            {
                if (isKeyDown && !_isSnowing) StartSnow();
                else if (!isKeyDown && _isSnowing) StopSnow();
            }
            else if (_configMode == 2) // 切换模式
            {
                if (isKeyDown && !_wasKeyDown) // 检测按下的瞬间
                {
                    if (_isSnowing) StopSnow();
                    else StartSnow();
                }
            }
            _wasKeyDown = isKeyDown;
        }

        private void StartSnow()
        {
            _isSnowing = true;
            SnowContainer.Visibility = Visibility.Visible;
            if (!_animationTimer.IsEnabled) _animationTimer.Start();
        }

        private void StopSnow()
        {
            _isSnowing = false;
            SnowContainer.Visibility = Visibility.Hidden;
            _animationTimer.Stop();
        }

        private void InitSnow()
        {
            for (int i = 0; i < 150; i++) CreateSnowflake();
        }

        private void CreateSnowflake()
        {
            double size = _random.Next(2, 7);
            // 简单优化：冻结画笔，节省一点点内存
            var brush = new SolidColorBrush(Colors.White);
            Ellipse ellipse = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = brush,
                Opacity = _random.NextDouble() * 0.5 + 0.3
            };
            double x = _random.NextDouble() * _screenWidth;
            double y = _random.NextDouble() * _screenHeight;
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);
            SnowContainer.Children.Add(ellipse);
            _snowflakes.Add(new Snowflake
            {
                UIElement = ellipse,
                X = x,
                Y = y,
                Speed = _random.NextDouble() * 2 + 1,
                Drift = _random.NextDouble() * 1 - 0.5
            });
        }

        // ============================================
        // 修复点 3：同理，修改事件参数为 object?
        // ============================================
        private void UpdateSnow(object? sender, EventArgs e)
        {
            foreach (var flake in _snowflakes)
            {
                flake.Y += flake.Speed;
                flake.X += flake.Drift;
                if (flake.Y > _screenHeight)
                {
                    flake.Y = -10;
                    flake.X = _random.NextDouble() * _screenWidth;
                }
                Canvas.SetLeft(flake.UIElement, flake.X);
                Canvas.SetTop(flake.UIElement, flake.Y);
            }
        }
    }

    public class Snowflake
    {
        // ============================================
        // 修复点 4：类属性未初始化警告
        // 使用 " = null!;" 默认赋值
        // ============================================
        public UIElement UIElement { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double Drift { get; set; }
    }
}