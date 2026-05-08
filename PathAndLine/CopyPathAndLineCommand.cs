using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.IO;
using Task = System.Threading.Tasks.Task;

namespace PathAndLine
{
    internal sealed class CopyPathAndLineCommand
    {
        // Must match guidPathAndLinePackageCmdSet in VSCommandTable.vsct.
        private static readonly Guid CommandSet = new Guid("579afcee-f77f-4c6c-9714-00a26c99b186");

        // Must match IDSymbol values in VSCommandTable.vsct.
        private const int FullPathCommandId     = 0x0100;
        private const int RelativePathCommandId = 0x0101;

        private readonly DTE2 _dte;
        private readonly PathAndLineOptions _options;

        private CopyPathAndLineCommand(OleMenuCommandService commandService, DTE2 dte, PathAndLineOptions options)
        {
            _dte = dte;
            _options = options;

            var fullPathItem = new OleMenuCommand(ExecuteFullPath, new CommandID(CommandSet, FullPathCommandId));
            fullPathItem.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(fullPathItem);

            var relativePathItem = new OleMenuCommand(ExecuteRelativePath, new CommandID(CommandSet, RelativePathCommandId));
            relativePathItem.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(relativePathItem);
        }

        public static async Task InitializeAsync(AsyncPackage package, DTE2 dte)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService))
                as OleMenuCommandService;

            if (commandService == null || dte == null)
                return;

            var options = ((PathAndLinePackage)package).Options;
            _ = new CopyPathAndLineCommand(commandService, dte, options);
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

        private void ExecuteFullPath(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.ActiveDocument == null)
                return;

            var fullPath = _dte.ActiveDocument.FullName;
            var selection = (TextSelection)_dte.ActiveDocument.Selection;
            var startLine = selection.TopPoint.Line;
            var endLine = selection.BottomPoint.Line;

            SetClipboard(FormatOutput(FormatPath(fullPath), startLine, endLine));
        }

        private void ExecuteRelativePath(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.ActiveDocument == null)
                return;

            var fullPath = _dte.ActiveDocument.FullName;
            var selection = (TextSelection)_dte.ActiveDocument.Selection;
            var startLine = selection.TopPoint.Line;
            var endLine = selection.BottomPoint.Line;

            var displayPath = GetRelativePathFromSolution(fullPath);

            SetClipboard(FormatOutput(FormatPath(displayPath), startLine, endLine));
        }

        /// <summary>
        /// Returns the file path relative to the open solution directory.
        /// Falls back to the filename alone when no solution is open or the file
        /// lies outside the solution tree.
        /// </summary>
        private string GetRelativePathFromSolution(string fullPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionFullName = _dte.Solution?.FullName;

            if (!string.IsNullOrEmpty(solutionFullName) && File.Exists(solutionFullName))
            {
                var solutionDir = Path.GetDirectoryName(solutionFullName);
                var relative = MakeRelativePath(solutionDir, fullPath);
                if (relative != null)
                    return relative;
            }

            return Path.GetFileName(fullPath);
        }

        /// <summary>
        /// Computes a relative path from <paramref name="fromDir"/> to <paramref name="toFile"/>
        /// using Uri arithmetic (compatible with .NET Framework 4.7.2).
        /// Returns null if the file is not under the base directory.
        /// </summary>
        private static string MakeRelativePath(string fromDir, string toFile)
        {
            if (string.IsNullOrEmpty(fromDir) || string.IsNullOrEmpty(toFile))
                return null;

            // Uri requires a trailing separator to treat fromDir as a directory.
            if (!fromDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                fromDir += Path.DirectorySeparatorChar;

            var baseUri = new Uri(fromDir);
            var fileUri = new Uri(toFile);

            if (baseUri.Scheme != fileUri.Scheme)
                return null;

            var relativeUri = baseUri.MakeRelativeUri(fileUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString())
                                  .Replace('/', Path.DirectorySeparatorChar);

            // If the relative path starts with ".." the file is outside the solution tree.
            if (relativePath.StartsWith(".."))
                return null;

            return relativePath;
        }

        private string FormatOutput(string path, int startLine, int endLine)
        {
            if (_options.UseMarkdownFormat)
            {
                var fileName = Path.GetFileName(path);
                var anchor = startLine == endLine ? $"#L{startLine}" : $"#L{startLine}-L{endLine}";
                return $"[{fileName}]({path}{anchor})";
            }

            var lineRef = startLine == endLine ? $"Line: {startLine}" : $"Lines: {startLine}-{endLine}";
            return $"{path} ({lineRef})";
        }

        private string FormatPath(string path) =>
            _options.UseUnixPaths ? path.Replace('\\', '/') : path;

        private static void SetClipboard(string text)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch
            {
                // Clipboard write failure: transient OS-level race. Silently swallow.
            }
        }
    }
}
