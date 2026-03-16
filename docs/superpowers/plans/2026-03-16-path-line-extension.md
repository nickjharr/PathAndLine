# Copy Path and Line — VS Extension Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Visual Studio 2022/2026 extension that adds "Copy path and line" to the text editor right-click menu, copying `C:\path\to\file.cs (Line: 42)` to the clipboard.

**Architecture:** Single VSIX project (net472) using AsyncPackage + DTE2. A `.vsct` file declares the command in the code editor context menu. `PathAndLinePackage` initialises on first code window activation and registers `CopyPathAndLineCommand`, which handles menu visibility and clipboard writing.

**Tech Stack:** C#, .NET Framework 4.7.2, Visual Studio SDK (Microsoft.VisualStudio.SDK NuGet), VSCT command table, DTE2 COM API, PresentationCore (System.Windows.Clipboard)

**Spec:** `docs/superpowers/specs/2026-03-16-path-line-extension-design.md`

**Prerequisites:** Visual Studio 2022 with the "Visual Studio extension development" workload installed. All build and test steps require this.

---

## Chunk 1: Project Scaffold and VSCT

### Task 1: Create the project file

**Files:**
- Create: `PathAndLineExtension.csproj`

This is an SDK-style `.csproj` for a VSIX targeting net472. The `VSCTCompile` item compiles the `.vsct` into the `Menus.ctmenu` resource that `[ProvideMenuResource]` expects.

- [ ] **Step 1.1: Create `PathAndLineExtension.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net472</TargetFramework>
    <GeneratePkgDefFile>true</GeneratePkgDefFile>
    <IncludeAssemblyInVSIXContainer>true</IncludeAssemblyInVSIXContainer>
    <IncludeDebugSymbolsInVSIXContainer>true</IncludeDebugSymbolsInVSIXContainer>
    <IncludeDebugSymbolsInLocalVSIXDeployment>true</IncludeDebugSymbolsInLocalVSIXDeployment>
    <CopyBuildOutputToOutputDirectory>true</CopyBuildOutputToOutputDirectory>
    <CopyOutputSymbolsToOutputDirectory>false</CopyOutputSymbolsToOutputDirectory>
    <!-- Required: activates VSSDK build targets (VSCT compilation, VSIX packaging, pkgdef generation) -->
    <IsVsixProject>true</IsVsixProject>
    <!-- F5 launches the VS Experimental Instance -->
    <StartAction>Program</StartAction>
    <StartProgram>$(DevEnvDir)devenv.exe</StartProgram>
    <StartArguments>/rootsuffix Exp</StartArguments>
    <RootNamespace>PathAndLineExtension</RootNamespace>
    <AssemblyName>PathAndLineExtension</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <!-- VS SDK meta-package for VS 2022 (17.x). Provides all Microsoft.VisualStudio.* references. -->
    <PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.31902.203" />
    <PackageReference Include="Microsoft.VSSDK.BuildTools" Version="17.10.2187" />
  </ItemGroup>

  <ItemGroup>
    <!-- Compiles VSCommandTable.vsct → Menus.ctmenu resource -->
    <VSCTCompile Include="VSCommandTable.vsct">
      <ResourceName>Menus.ctmenu</ResourceName>
    </VSCTCompile>
  </ItemGroup>

  <ItemGroup>
    <Content Include="source.extension.vsixmanifest">
      <SubType>Designer</SubType>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <!-- Required for System.Windows.Clipboard -->
    <Reference Include="PresentationCore" />
    <Reference Include="WindowsBase" />
  </ItemGroup>

</Project>
```

- [ ] **Step 1.2: Commit**

```bash
git add PathAndLineExtension.csproj
git commit -m "chore: add VSIX project file"
```

---

### Task 2: Generate two GUIDs and record them

Two GUIDs are required throughout the project. Generate both now and use them consistently everywhere. Reusing the same GUID for both causes silent registration failures.

**Files:**
- No files yet — just generate values to paste in subsequent tasks.

- [ ] **Step 2.1: Generate GUID 1 — Package GUID**

Use PowerShell: `[System.Guid]::NewGuid()` or any online GUID generator.
Format: `{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}` (with braces, uppercase).
Record as: **PACKAGE_GUID**

- [ ] **Step 2.2: Generate GUID 2 — Command Set GUID**

Same method — a completely different GUID.
Format: `{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}`
Record as: **COMMAND_SET_GUID**

---

### Task 3: Create the VSCT command table

**Files:**
- Create: `VSCommandTable.vsct`

The `.vsct` file tells VS: where the command lives in the UI (`IDM_VS_CTXMENU_CODEWIN`), what it's called, and that it supports dynamic visibility (`DynamicVisibility` flag — **required** for `BeforeQueryStatus` to fire; without it the item always shows).

- [ ] **Step 3.1: Create `VSCommandTable.vsct`**

Replace `PACKAGE_GUID` and `COMMAND_SET_GUID` with the values from Task 2. Include the curly braces.

```xml
<?xml version="1.0" encoding="utf-8"?>
<CommandTable xmlns="http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable"
              xmlns:xs="http://www.w3.org/2001/XMLSchema">

  <Extern href="stdidcmd.h"/>
  <Extern href="vsshlids.h"/>

  <Commands package="guidPathAndLinePackage">

    <Groups>
      <!--
        Priority 0x0600 places this group after the default editor groups.
        Parent is the standard code editor right-click menu.
        Verify IDM_VS_CTXMENU_CODEWIN in:
        %VSINSTALLDIR%\VSSDK\VisualStudioIntegration\Common\Inc\vsshlids.h
      -->
      <Group guid="guidPathAndLinePackageCmdSet" id="CopyPathAndLineGroup" priority="0x0600">
        <Parent guid="guidSHLMainMenu" id="IDM_VS_CTXMENU_CODEWIN"/>
      </Group>
    </Groups>

    <Buttons>
      <Button guid="guidPathAndLinePackageCmdSet" id="CopyPathAndLineCommandId"
              priority="0x0100" type="Button">
        <Parent guid="guidPathAndLinePackageCmdSet" id="CopyPathAndLineGroup"/>
        <!--
          DynamicVisibility: REQUIRED. Without this flag VS will not call
          BeforeQueryStatus and the item will always be visible.
        -->
        <CommandFlag>DynamicVisibility</CommandFlag>
        <Strings>
          <ButtonText>Copy path and line</ButtonText>
        </Strings>
      </Button>
    </Buttons>

  </Commands>

  <Symbols>
    <GuidSymbol name="guidPathAndLinePackage" value="{PACKAGE_GUID}" />
    <GuidSymbol name="guidPathAndLinePackageCmdSet" value="{COMMAND_SET_GUID}">
      <IDSymbol name="CopyPathAndLineGroup"     value="0x1020" />
      <IDSymbol name="CopyPathAndLineCommandId" value="0x0100" />
    </GuidSymbol>
  </Symbols>

</CommandTable>
```

- [ ] **Step 3.2: Commit**

```bash
git add VSCommandTable.vsct
git commit -m "feat: add VSCT command table for Copy path and line"
```

---

### Task 4: Create the VSIX manifest

**Files:**
- Create: `source.extension.vsixmanifest`

The manifest declares the package identity, VS version targets, and author metadata. The `Identity Id` must be the **package GUID** (PACKAGE_GUID from Task 2, without braces). Repeat `InstallationTarget` for each VS edition you want to support.

- [ ] **Step 4.1: Create `source.extension.vsixmanifest`**

Replace `PACKAGE_GUID_NO_BRACES` with PACKAGE_GUID from Task 2 **without** the surrounding `{ }`.

```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011"
    xmlns:d="http://schemas.microsoft.com/developer/vsx-schema-design/2011">
  <Metadata>
    <Identity Id="PACKAGE_GUID_NO_BRACES"
              Version="1.0.0"
              Language="en-US"
              Publisher="YourName" />
    <DisplayName>Copy Path and Line</DisplayName>
    <Description>Adds a "Copy path and line" command to the text editor context menu.</Description>
  </Metadata>
  <Installation>
    <!--
      Version range [17.0, 19.0) covers VS 2022 (17.x) and VS 2026 (18.x).
      TODO: confirm upper bound once VS 2026 SDK is publicly released.
      Repeat for Professional and Enterprise if needed.
    -->
    <InstallationTarget Id="Microsoft.VisualStudio.Community"    Version="[17.0, 19.0)" />
    <InstallationTarget Id="Microsoft.VisualStudio.Professional" Version="[17.0, 19.0)" />
    <InstallationTarget Id="Microsoft.VisualStudio.Enterprise"   Version="[17.0, 19.0)" />
  </Installation>
  <Dependencies>
    <Dependency Id="Microsoft.Framework.NDP"
                DisplayName=".NET Framework"
                d:Source="Auto"
                Version="[4.7.2,)" />
  </Dependencies>
  <Assets>
    <Asset Type="Microsoft.VisualStudio.VsPackage"
           d:Source="Project"
           d:ProjectName="%CurrentProject%"
           Path="|PathAndLineExtension;PkgdefProjectOutputGroup|" />
  </Assets>
</PackageManifest>
```

- [ ] **Step 4.2: Commit**

```bash
git add source.extension.vsixmanifest
git commit -m "feat: add VSIX manifest targeting VS 2022 and 2026"
```

---

## Chunk 2: Package and Command Classes

### Task 5: Implement `PathAndLinePackage`

**Files:**
- Create: `PathAndLinePackage.cs`

This is a minimal `AsyncPackage`. Its only job is to switch to the UI thread and initialize the command. The four attributes are all required — omitting any one causes silent failures.

- [ ] **Step 5.1: Create `PathAndLinePackage.cs`**

Replace `PACKAGE_GUID` with the value from Task 2 (with braces).

```csharp
using System;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace PathAndLineExtension
{
    /// <summary>
    /// The package. Loads once the first time a code editor window becomes active.
    /// Its only job is to initialize CopyPathAndLineCommand.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.CodeWindow_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuidString)]
    public sealed class PathAndLinePackage : AsyncPackage
    {
        // Replace with PACKAGE_GUID (no braces, no hyphens formatting — just the raw string).
        // Example: "12345678-1234-1234-1234-123456789012"
        public const string PackageGuidString = "PACKAGE_GUID_NO_BRACES";

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            // Switch to UI thread before any COM or command registration.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Acquire DTE2 on the UI thread and pass to the command.
            var dte = await GetServiceAsync(typeof(DTE)) as DTE2;

            await CopyPathAndLineCommand.InitializeAsync(this, dte);
        }
    }
}
```

> **Note on `PackageGuidString`:** Use the PACKAGE_GUID from Task 2, without braces, as a plain string literal.
> E.g. if your GUID is `{A1B2C3D4-...}`, write `"a1b2c3d4-..."`.

- [ ] **Step 5.2: Commit**

```bash
git add PathAndLinePackage.cs
git commit -m "feat: add PathAndLinePackage AsyncPackage"
```

---

### Task 6: Implement `CopyPathAndLineCommand`

**Files:**
- Create: `CopyPathAndLineCommand.cs`

This class owns the `OleMenuCommand`. Key rules from the spec:
- `BeforeQueryStatus` runs on the UI thread (VS guarantees this for OleMenuCommand). Always set `Visible = false` first (it's sticky — won't reset automatically between queries).
- Unsaved-file detection: `!File.Exists(FullName)` — unambiguous regardless of what VS puts in `FullName` for unsaved docs.
- `Execute` is also on the UI thread. Null-check `ActiveDocument` defensively (race guard).
- `Clipboard.SetText` requires STA (UI thread) — already satisfied.

- [ ] **Step 6.1: Create `CopyPathAndLineCommand.cs`**

Replace `COMMAND_SET_GUID` with the value from Task 2 (no braces, plain string).

```csharp
using System;
using System.ComponentModel.Design;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace PathAndLineExtension
{
    /// <summary>
    /// Handles the "Copy path and line" context menu command.
    /// </summary>
    internal sealed class CopyPathAndLineCommand
    {
        // Replace with COMMAND_SET_GUID (no braces).
        // Must match guidPathAndLinePackageCmdSet in VSCommandTable.vsct.
        private static readonly Guid CommandSet = new Guid("COMMAND_SET_GUID_NO_BRACES");

        // Must match IDSymbol CopyPathAndLineCommandId in VSCommandTable.vsct (0x0100 = 256).
        private const int CommandId = 0x0100;

        private readonly DTE2 _dte;

        private CopyPathAndLineCommand(OleMenuCommandService commandService, DTE2 dte)
        {
            _dte = dte;

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandId);
            menuItem.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package, DTE2 dte)
        {
            // Already on UI thread (called from PathAndLinePackage.InitializeAsync
            // after SwitchToMainThreadAsync).
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(
                package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService))
                as OleMenuCommandService;

            _ = new CopyPathAndLineCommand(commandService, dte);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var command = (OleMenuCommand)sender;

            // Always reset — OleMenuCommand.Visible is sticky and will not
            // clear automatically between context menu openings.
            command.Visible = false;

            if (_dte.ActiveDocument == null)
                return;

            if (!File.Exists(_dte.ActiveDocument.FullName))
                return;

            command.Visible = true;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Defensive null check — guards against race between BeforeQueryStatus
            // and the user clicking.
            if (_dte.ActiveDocument == null)
                return;

            var fullPath = _dte.ActiveDocument.FullName;
            var selection = (TextSelection)_dte.ActiveDocument.Selection;
            var lineNumber = selection.ActivePoint.Line;

            var text = $"{fullPath} (Line: {lineNumber})";

            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch
            {
                // Clipboard write failure: transient OS-level race.
                // Silently swallow — not worth surfacing for a simple copy operation.
            }
        }
    }
}
```

- [ ] **Step 6.2: Commit**

```bash
git add CopyPathAndLineCommand.cs
git commit -m "feat: implement CopyPathAndLineCommand with visibility guard and clipboard write"
```

---

## Chunk 3: Build, Install, and Smoke Test

### Task 7: First build

No automated tests exist for this extension (DTE2 interop requires a live VS instance). Build verification is the first gate.

**Files:** None new.

- [ ] **Step 7.1: Open the solution in Visual Studio 2022**

Open `PathAndLineExtension.csproj` in Visual Studio 2022 (not VS Code).

- [ ] **Step 7.2: Restore NuGet packages**

Build menu → Restore NuGet Packages, or run in Developer Command Prompt:

```
msbuild PathAndLineExtension.csproj /t:Restore
```

- [ ] **Step 7.3: Build the project**

Build menu → Build Solution (`Ctrl+Shift+B`).

Expected: **0 errors, 0 warnings** (or warnings only about VS SDK deprecations — those are safe to ignore).

If there are errors:
- `Cannot find 'Menus.ctmenu'`: check `VSCTCompile` entry in `.csproj` and that `VSCommandTable.vsct` exists.
- `GUID format` errors: confirm GUID strings are lowercase, no braces, correct format in `.cs` files.
- `PresentationCore` not found: confirm the `<Reference Include="PresentationCore" />` line is in `.csproj`.

- [ ] **Step 7.4: Commit if any fixes were needed**

```bash
git add -u
git commit -m "fix: resolve build errors"
```

---

### Task 8: Install into the Experimental Instance and smoke test

VS extension development uses an "Experimental Instance" — a sandboxed copy of VS that your extension is deployed into for testing. F5 launches it automatically.

**Files:** None.

- [ ] **Step 8.1: Launch the Experimental Instance**

Press **F5** in Visual Studio (or Debug → Start Debugging).

A second copy of Visual Studio opens. The title bar will say "Microsoft Visual Studio (Experimental Instance)".

- [ ] **Step 8.2: Smoke test 1 — command appears for a saved file**

In the Experimental Instance:
1. Open any saved `.cs` file (e.g. `File > Open > File`).
2. Right-click inside the editor.

Expected: "Copy path and line" appears in the context menu.
If not: check `DynamicVisibility` flag in `.vsct` and that `[ProvideAutoLoad]` uses `VSConstants.UICONTEXT.CodeWindow_string`.

- [ ] **Step 8.3: Smoke test 2 — correct output**

1. Place cursor on a known line (e.g. line 5).
2. Click "Copy path and line".
3. Paste into Notepad (`Ctrl+V`).

Expected output: `C:\full\path\to\file.cs (Line: 5)`

Verify: path uses backslashes, format matches exactly `{path} (Line: {lineNumber})`.

- [ ] **Step 8.4: Smoke test 3 — line number tracks cursor**

1. Move cursor to line 1 → copy → verify `(Line: 1)`.
2. Move cursor to line 100 (if file has 100+ lines) → copy → verify `(Line: 100)`.
3. Move cursor to last line → copy → verify correct number.

- [ ] **Step 8.5: Smoke test 4 — hidden for unsaved file**

1. `File > New > File` (creates an Untitled buffer).
2. Right-click in the editor.

Expected: "Copy path and line" is **not visible** in the context menu.

- [ ] **Step 8.6: Smoke test 5 — null guard (code inspection)**

Review `BeforeQueryStatus` in `CopyPathAndLineCommand.cs`:
- First line inside the method must set `command.Visible = false`.
- Next check must be `if (_dte.ActiveDocument == null) return;`.
- Next check must be `if (!File.Exists(_dte.ActiveDocument.FullName)) return;`.

This guard cannot be triggered via right-click (the context menu requires an open editor), so code inspection is the verification.

- [ ] **Step 8.7: Commit smoke test sign-off note**

```bash
git commit --allow-empty -m "chore: smoke tests passed — command visible/hidden correctly, output format verified"
```

---

### Task 9: Package the VSIX for distribution

Produces a `.vsix` file that can be manually installed on any machine with VS 2022 or VS 2026.

**Files:** None new.

- [ ] **Step 9.1: Build in Release configuration**

In Visual Studio: change configuration to **Release** (dropdown in toolbar), then Build → Build Solution.

Or from Developer Command Prompt:
```
msbuild PathAndLineExtension.csproj /p:Configuration=Release
```

- [ ] **Step 9.2: Locate the VSIX file**

The output will be at:
```
bin\Release\PathAndLineExtension.vsix
```

- [ ] **Step 9.3: Install on target machine**

Double-click `PathAndLineExtension.vsix` → VS installer opens → Install.
Restart Visual Studio.
Verify the command appears in the editor context menu.

- [ ] **Step 9.4: Final commit**

```bash
git add -u
git commit -m "chore: release build verified, VSIX ready for distribution"
```
