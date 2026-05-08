# Copy Path and Line

A Visual Studio 2022/2026 extension that adds **Copy full path and line number** and **Copy relative path and line number** commands to the code editor right-click context menu.

## What it does

Right-clicking in any open source file gives you two entries:

**Copy full path and line number** — copies the absolute file path and cursor line number:
```
C:\Dev\MyProject\src\Foo.cs (Line: 42)
```

**Copy relative path and line number** — copies the path relative to the open solution directory and cursor line number:
```
src\Foo.cs (Line: 42)
```

When text spanning multiple lines is selected, both commands capture the full range:
```
src\Foo.cs (Lines: 42-58)
```

If no solution is open, the relative command falls back to the filename alone. Both commands are hidden for unsaved (untitled) files.

## Installation

Install the `.vsix` file directly:

1. Close Visual Studio
2. Double-click `PathAndLine.vsix`
3. Follow the installer prompts
4. Reopen Visual Studio

## Keyboard shortcuts

Both commands can be bound to keyboard shortcuts via **Tools → Options → Environment → Keyboard**:

- `EditorContextMenus.CodeWindow.Copyfullpathandlinenumber`
- `EditorContextMenus.CodeWindow.Copyrelativepathandlinenumber`

## Configuration

Open **Tools → Options → Copy Path and Line → General** to configure the extension.

| Setting | Default | Description |
|---------|---------|-------------|
| Use Unix-style paths | Off | When enabled, path separators are forward slashes (`/`) instead of backslashes (`\`). Always enabled when Markdown format is on. |
| Use Markdown link format | Off | When enabled, output is a Markdown link instead of plain text. Automatically enables Unix-style paths. |

**Example output with Unix-style paths enabled:**
```
C:/Dev/MyProject/src/Foo.cs (Line: 42)
src/Foo.cs (Lines: 42-58)
```

**Example output with Markdown link format enabled:**
```
[Foo.cs](C:/Dev/MyProject/src/Foo.cs#L42)
[Foo.cs](src/Foo.cs#L42-L58)
```

The Markdown anchor syntax (`#L42`, `#L42-L58`) is recognised by GitHub, GitLab, VS Code, and AI coding tools such as OpenAI Codex.

## Supported versions

- Visual Studio 2022 (17.x)
- Visual Studio 2026 (18.x)

## Building from source

Open `PathAndLine.sln` in Visual Studio 2022 and build. The `.vsix` is written to `PathAndLine/bin/Debug/` or `bin/Release/` depending on configuration.

To test in an isolated experimental instance, set the startup project to **PathAndLine** and press **F5** — Visual Studio launches a sandboxed instance with the extension loaded.
