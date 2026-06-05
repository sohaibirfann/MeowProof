# MeowProof

A lightweight Windows system-tray utility that locks your keyboard so your cat
can't interfere while you step away. Lock from the tray, the main window, or a
global hotkey; a friendly full-screen overlay shows while locked.

- **Platform:** Windows 10/11
- **Stack:** C# 12 · .NET 8 · WPF (+ WinForms `NotifyIcon` for the tray)

## Build & run

```
dotnet build
dotnet run
```

The app starts in the system tray and opens its main window. Right-click the
tray icon for the menu (Lock, Stealth Lock, Settings, Quit).

## Features

- Lock / unlock the keyboard
- Stealth Lock (silent, no overlay)
- Customizable global unlock shortcut
- Full-screen overlay while locked
- Prevent display sleep while locked
- Launch at startup

> Status: in active development.
