using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace PathAndLine
{
    public sealed class PathAndLineOptions : DialogPage
    {
        [Category("Path Format")]
        [DisplayName("Use Unix-style paths")]
        [Description("When enabled, path separators are forward slashes (/) instead of backslashes (\\).")]
        public bool UseUnixPaths { get; set; } = false;
    }
}
