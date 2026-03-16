# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

A Visual Studio 2022/2026 VSIX extension that adds two commands to the code editor right-click menu: **Copy full path and line number** (absolute path) and **Copy relative path and line number** (relative to the solution directory). Both produce output in the format `path\to\file.cs (Line: 42)`.

## Build Commands

**Build the VSIX (command line):**
```bash
msbuild PathAndLine/PathAndLine.csproj /p:Configuration=Release
```
Output: `PathAndLine/bin/Release/PathAndLine.vsix`

**Restore NuGet packages:**
```bash
msbuild PathAndLine/PathAndLine.csproj /t:Restore
```

**Test:** Open `PathAndLine.sln` in Visual Studio 2022 and press F5. This launches an isolated Experimental Instance of VS with the extension loaded. The VSIX is written to `PathAndLine/bin/Debug/PathAndLine.vsix`. There are no automated tests — DTE2 interop requires a live VS instance.

The two commands can be bound to keyboard shortcuts via **Tools → Options → Environment → Keyboard** using these names:
- `EditorContextMenus.CodeWindow.Copyfullpathandlinenumber`
- `EditorContextMenus.CodeWindow.Copyrelativepathandlinenumber`

## Architecture

**Stack:** C# / .NET Framework 4.7.2 / Visual Studio SDK v17 (AsyncPackage model). Classic framework is required — the modern out-of-process `net8.0-windows + VisualStudio.Extensibility` model is intentionally not used.

**Two source files do everything:**

- **`PathAndLinePackage.cs`** — `AsyncPackage` subclass. Initializes on first code window activation (`[ProvideAutoLoad]`), switches to the UI thread, acquires the `DTE2` COM object, and passes it to the command. No business logic.

- **`CopyPathAndLineCommand.cs`** — Registers two `OleMenuCommand` instances (IDs `0x0100` and `0x0101`). Both share a `BeforeQueryStatus` handler that resets `Visible = false` then shows the item only when `ActiveDocument` has a saved file path. `ExecuteFullPath` copies the absolute path; `ExecuteRelativePath` computes the path relative to the solution directory via `Uri.MakeRelativeUri` (.NET 4.7.2-compatible), falling back to filename alone when no solution is open or the file is outside the solution tree. Clipboard failures are silently swallowed.

**Supporting files:**
- `VSCommandTable.vsct` — Declares command placement in `IDG_VS_CODEWIN_TEXTEDIT` (code editor right-click group) with `DynamicVisibility` flag. Without `DynamicVisibility`, `BeforeQueryStatus` never fires.
- `source.extension.vsixmanifest` — VSIX metadata; targets VS 2022–2026 (v17.0–19.0).

## Critical Constraints

**GUID management:** Two distinct GUIDs are used — one for the package, one for the command set. They appear in `PathAndLinePackage.cs`, `CopyPathAndLineCommand.cs`, `VSCommandTable.vsct`, and `source.extension.vsixmanifest`. Reusing the same GUID for both causes silent registration failure with no error message.

**Threading:** All DTE2/clipboard calls must happen on the VS UI thread. Never wrap `Execute` body in `Task.Run` — it causes COM marshaling failures.

**Visibility state is sticky:** `OleMenuCommand.Visible` persists between calls. You must explicitly set it to `false` at the top of every `BeforeQueryStatus` handler before conditionally setting it back to `true`.
