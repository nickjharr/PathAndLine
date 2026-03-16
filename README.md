# Copy Path and Line

A Visual Studio 2022/2026 extension that adds a **Copy path and line number** command to the code editor right-click context menu.

## What it does

Right-clicking in any open source file gives you a **Copy path and line number** entry. Clicking it copies the file's full path and the current cursor line number to the clipboard:

```
C:\Dev\MyProject\src\Foo.cs (Line: 42)
```

The command is hidden for unsaved (untitled) files.

## Installation

Install the `.vsix` file directly:

1. Close Visual Studio
2. Double-click `PathAndLine.vsix`
3. Follow the installer prompts
4. Reopen Visual Studio

## Keyboard shortcut

The command is registered as `EditorContextMenus.CodeWindow.Copypathandlinenumber` and can be bound to a keyboard shortcut via **Tools → Options → Environment → Keyboard**.

## Supported versions

- Visual Studio 2022 (17.x)
- Visual Studio 2026 (18.x)

## Building from source

Open `PathAndLine.sln` in Visual Studio 2022 and build. The `.vsix` is written to `PathAndLine/bin/Debug/` or `bin/Release/` depending on configuration.

To test in an isolated experimental instance, set the startup project to **PathAndLine** and press **F5** — Visual Studio launches a sandboxed instance with the extension loaded.
