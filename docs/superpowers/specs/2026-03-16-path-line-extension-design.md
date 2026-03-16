# Design: Copy Path and Line — Visual Studio Extension

**Date:** 2026-03-16
**Status:** Draft
**Target:** Visual Studio 2022 and 2026 (VSIX, manual distribution)

---

## Overview

A Visual Studio extension that adds a "Copy path and line" entry to the text editor right-click context menu. Clicking it copies the current file's full path and cursor line number to the clipboard in the format:

```
C:\Path\To\File.cs (Line: 42)
```

---

## Architecture

A single VSIX project targeting `net472`, using the classic AsyncPackage/DTE2 approach. No external dependencies beyond the VS SDK NuGet packages.

```
PathAndLineExtension.csproj          — VSIX project (net472)
├── source.extension.vsixmanifest    — package metadata, VS version targets
├── PathAndLinePackage.cs            — AsyncPackage, registers the command
├── CopyPathAndLineCommand.cs        — command logic (visibility + clipboard write)
└── VSCommandTable.vsct              — declares command, group, menu placement
```

**Target framework:** `net472`. The `net8.0-windows` TFM is not used here — it is associated with the new `VisualStudio.Extensibility` out-of-process model, which was explicitly not chosen. The classic `AsyncPackage` + DTE2 approach requires `net472`.

---

## Components

### `PathAndLinePackage`

Minimal `AsyncPackage` subclass. Initialises `CopyPathAndLineCommand` during `InitializeAsync`. Decorated with:
- `[PackageRegistration]`
- `[ProvideMenuResource("Menus.ctmenu", 1)]` — the string `"Menus.ctmenu"` must match the resource name the VS build system assigns when compiling the `.vsct` file. The default name for a VSIX project is `"Menus.ctmenu"` regardless of the `.vsct` filename. Verify in the compiled `.pkgdef` or project build output if the menu resource is not found at runtime.
- `[ProvideAutoLoad(UIContextGuids80.CodeWindow, PackageAutoLoadFlags.BackgroundLoad)]` — loads the package **once**, the first time a code editor window becomes active. `CodeWindow` is preferred over `SolutionExists` so the command works when opening a single file without a solution. No additional manifest configuration is required; the attribute handles registry registration automatically.
- `[Guid("...")]` — the **package GUID**: a unique GUID generated for this package. **This same GUID must be consistent across three locations:** the `[Guid]` attribute on the class, the `<Identity Id=` field in `source.extension.vsixmanifest`, and the `GuidSymbol` named `guidPathAndLinePackage` in the `.vsct`.

**GUIDs — two are required:**
1. **Package GUID** — identifies the package itself. Appears in `[Guid]` on the class, the `<Identity Id=` field in the `.vsixmanifest`, and the `GuidSymbol` named `guidPathAndLinePackage` in the `.vsct`.
2. **Command set GUID** — a separate, independently generated GUID that scopes the command ID integer. Used in the `GuidSymbol` named `guidPathAndLinePackageCmdSet` in the `.vsct` and when constructing the `CommandID` in code. Reusing the package GUID for both is a common mistake that causes silent registration failures.

Use `Tools > Create GUID` in Visual Studio or any online GUID generator for each.

**`InitializeAsync` threading:** `InitializeAsync` runs on a background thread when `BackgroundLoad` is set. The sequence must be:
1. `await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)` — switch to UI thread.
2. Acquire `DTE2` via `GetService(typeof(DTE)) as DTE2` and store it as a field on the command. DTE2 must be fetched on the UI thread and stored — never retrieved on-demand inside `BeforeQueryStatus` or `Execute`.
3. Acquire `OleMenuCommandService` via `GetService(typeof(IMenuCommandService)) as OleMenuCommandService`.
4. Construct and register the `OleMenuCommand`.

No other responsibilities.

### `CopyPathAndLineCommand`

Owns a stored `DTE2` reference (set at construction time from `InitializeAsync`) and an `OleMenuCommand` with two responsibilities:

1. **`BeforeQueryStatus`** — Called by VS on the **UI thread** (VS guarantees this for `OleMenuCommand` handlers; COM property access on the stored `DTE2` field is safe). Logic:
   - Set `command.Visible = false` as the default at the top of the handler (prevents stale `true` state from a previous query, since `OleMenuCommand.Visible` is sticky and not reset between queries).
   - If `_dte.ActiveDocument` is null → return (leaving `Visible = false`).
   - If `!System.IO.File.Exists(_dte.ActiveDocument.FullName)` → return (unsaved file; `FullName` may be any non-path string for unsaved docs).
   - Otherwise → set `command.Visible = true`.
   - **`Visible` must be explicitly assigned on every code path.**

2. **`Execute`** — Called on the UI thread by VS command infrastructure. Do not wrap in `Task.Run`.
   - Guard: if `_dte.ActiveDocument` is null, return silently (defensive against race between menu query and click).
   - Reads `_dte.ActiveDocument.FullName` for the file path (Windows-native backslashes, provided natively by DTE2).
   - Reads `((TextSelection)_dte.ActiveDocument.Selection).ActivePoint.Line` for the 1-based cursor line number.
   - Formats: `{path} (Line: {lineNumber})`
   - Writes to clipboard via `System.Windows.Clipboard.SetText(...)` from `PresentationCore.dll`. Add `PresentationCore` as an explicit assembly reference in the `.csproj` — it is not included by default in VSIX project templates but is available in the `net472` GAC. `System.Windows.Forms.Clipboard` is an equivalent alternative if `System.Windows.Forms` is already referenced; if not, prefer `PresentationCore`.

### `VSCommandTable.vsct`

Declares:
- A `Symbols` section defining two `GuidSymbol` entries: one for the package GUID (`guidPathAndLinePackage`) and one for the command set GUID (`guidPathAndLinePackageCmdSet`). See GUID note above.
- A command group parented to `guidSHLMainMenu` / `IDM_VS_CTXMENU_CODEWIN` — the standard text editor right-click menu. To find the integer value of `IDM_VS_CTXMENU_CODEWIN`, look it up in `vsshlids.h` at: `%VSINSTALLDIR%\VSSDK\VisualStudioIntegration\Common\Inc\vsshlids.h`.
- The group `Priority` value controls placement in the menu. Use `0x0600` as a starting point (places the group after the default editor groups). Adjust as needed during testing.
- A single command with label `Copy path and line`, assigned an integer ID (e.g. `0x0100`) scoped to the command set GUID.
- The command **must include `CommandFlag DynamicVisibility`** in the `.vsct`. Without this flag, VS will not call `BeforeQueryStatus` and the item will always be visible regardless of the handler logic.

---

## Data Flow

```
User right-clicks in editor
        │
        ▼
VS calls BeforeQueryStatus (on UI thread)
        │
        ├─ Default: Visible = false
        ├─ ActiveDocument is null?                          → return (Visible stays false)
        ├─ !File.Exists(ActiveDocument.FullName)?           → return (unsaved, Visible stays false)
        │
        └─ Valid saved document
                  └─ Visible = true → "Copy path and line" shown
                            │
                            ▼
                    User clicks the item
                            │
                            ▼
                    Execute fires (on UI thread)
                            │
                    Null-check ActiveDocument (race guard)
                    Read FullName (path)
                    Read ActivePoint.Line (line number)
                    Format string
                    Write to Clipboard (UI thread, STA)
```

---

## Error Handling

| Scenario | Handling |
|---|---|
| Unsaved / Untitled file | `BeforeQueryStatus` checks `!File.Exists(FullName)`, hides item |
| No active document | `BeforeQueryStatus` null-checks `ActiveDocument`, hides item |
| Race: document closed between query and click | `Execute` null-checks `ActiveDocument`, returns silently |
| Clipboard write failure | Wrapped in try/catch, silently swallowed. Rationale: clipboard failures are transient OS-level races; surfacing them adds complexity disproportionate to the failure frequency for a simple copy operation. |
| Line number format | DTE2 `ActivePoint.Line` is 1-based — matches expected output |

---

## VS Version Compatibility

- **Target versions:** Visual Studio 2022 (17.x) and Visual Studio 2026 (18.x).
- The VSIX manifest `InstallationTarget` must include an entry per edition. Example for one edition:
  ```xml
  <InstallationTarget Id="Microsoft.VisualStudio.Community" Version="[17.0, 19.0)" />
  ```
  Add corresponding entries for `Microsoft.VisualStudio.Professional` and `Microsoft.VisualStudio.Enterprise` if targeting those editions. The `Id` attribute is required — omitting it produces a manifest that silently targets nothing.
- **Version range `[17.0, 19.0)` is provisional.** VS 2022 = 17.x, VS 2026 = 18.x. **TODO:** confirm the upper bound once the VS 2026 SDK NuGet packages are publicly available and update accordingly.
- Implementation uses `AsyncPackage` and DTE2 — both are stable across VS 2022 and expected to remain supported in VS 2026.

---

## Testing

Automated unit testing is not included in this iteration. The interesting code is entirely DTE2 interop, which requires a live VS instance to test meaningfully.

### Manual Smoke Tests

| # | Scenario | Expected Result |
|---|---|---|
| 1 | Open a saved `.cs` file, right-click in the editor | "Copy path and line" appears in context menu |
| 2 | Click "Copy path and line" | Clipboard contains `C:\path\to\file.cs (Line: N)` with correct path and line |
| 3 | Move cursor to line 1, line 100, and last line of file; copy each | Line number in clipboard matches cursor position each time |
| 4 | Open an unsaved Untitled file (`File > New > File`), right-click in the editor | "Copy path and line" is **not** visible |
| 5 | *(Defensive null guard — not directly triggerable via right-click since the code editor context menu requires an open editor.)* Verify by code inspection that `BeforeQueryStatus` sets `Visible = false` by default and null-checks `ActiveDocument` before calling `File.Exists`. |

---

## Out of Scope

- VSCode support
- Marketplace publishing (deferred)
- Multi-selection / range copy
- Configurable output format
