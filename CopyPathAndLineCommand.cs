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
        // Must match guidPathAndLinePackageCmdSet in VSCommandTable.vsct.
        private static readonly Guid CommandSet = new Guid("579afcee-f77f-4c6c-9714-00a26c99b186");

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

            if (commandService == null || dte == null)
                return;

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
