using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace PathAndLine
{
    public sealed class PathAndLineOptions : DialogPage
    {
        private bool _useUnixPaths;

        [Category("Path Format")]
        [DisplayName("Use Unix-style paths")]
        [Description("When enabled, path separators are forward slashes (/). Always enabled when Markdown format is on.")]
        public bool UseUnixPaths
        {
            get => UseMarkdownFormat || _useUnixPaths;
            set => _useUnixPaths = value;
        }

        [Category("Path Format")]
        [DisplayName("Use Markdown link format")]
        [Description("When enabled, copies a Markdown link — [file.cs](path#L42) — instead of plain text. Automatically enables Unix-style paths.")]
        public bool UseMarkdownFormat { get; set; }
    }
}
