using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DesktopSnow
{
    public partial class MainWindow : Window
    {
        // 引用系统API用于监听按键
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

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
            Ellipse ellipse = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = Brushes.White,
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