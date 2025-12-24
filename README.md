# ❄️ DesktopSnow (Ghost Edition)

A lightweight, transparent Windows desktop snow effect application using WPF.
一个轻量级、全透明的 Windows 桌面下雪特效程序。

## 👻 幽灵模式说明 (Ghost Mode)

**注意 (Attention):** 此程序设计为 **“幽灵模式” (Ghost Mode)**。
* 运行后 **任务栏没有图标** (No Taskbar Icon)。
* 运行后 **托盘区没有图标** (No System Tray Icon)。
* 它是完全隐形的，只在屏幕上下雪。

## 🎮 快捷键 (Controls)

由于没有界面，请务必记住以下控制键：

| 按键 (Key) | 功能 (Function) | 说明 (Note) |
| :--- | :--- | :--- |
| **F9** | 开/关 雪花 (Toggle) | 也可以在 `config.txt` 中自定义 |
| **F12** | **彻底退出 (Exit)** | **紧急停止键，按下即杀进程** |

> 如果忘记快捷键，可以通过“任务管理器”结束 `DesktopSnow.exe` 进程。

## ⚙️ 配置 (Configuration)

在程序同目录下创建 `config.txt` 文件进行配置：

```ini
Mode=2            ; 1=按住显示, 2=切换显示(默认)
StartupShow=true  ; true=启动即下雪, false=启动等待
StartupDuration=5 ; 启动后自动下雪几秒钟
Key=120           ; 快捷键代码 (120=F9)
