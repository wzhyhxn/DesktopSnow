# ❄️ DesktopSnow (Ghost Edition)

[🇨🇳 中文说明 (Chinese)](README.zh-CN.md) | 🇺🇸 English

A lightweight, transparent Windows desktop snow effect application using WPF.

## 👻 Ghost Mode Explanation

**Attention:** This program is designed as **"Ghost Mode"**.
* **No Taskbar Icon** when running.
* **No System Tray Icon** when running.
* It is completely invisible, only rendering snow on your screen.

## 🎮 Controls

Since there is no visible UI, please memorize these controls:

| Key | Function | Note |
| :--- | :--- | :--- |
| **F9** | Toggle Snow | Can be customized in `config.txt` |
| **F12** | **Emergency Exit** | **Instantly kills the process** |

> If you forget the keys, use "Task Manager" to kill `DesktopSnow.exe`.

## ⚙️ Configuration

Create a `config.txt` file in the same folder as the `.exe`:

```ini
Mode=2            ; 1=Hold to show, 2=Toggle (Default)
StartupShow=true  ; true=Snow on start, false=Wait for key
StartupDuration=5 ; Duration of auto-snow on startup (seconds)
Key=120           ; Key code for toggle (120=F9)
```
🚀 Auto Start
1. Create a shortcut of DesktopSnow.exe.
2. Press Win + R, type shell:startup.
3. Paste the shortcut into the folder.
