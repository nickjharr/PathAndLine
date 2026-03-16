using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace PathAndLine
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.CodeWindow_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PathAndLinePackage.PackageGuidString)]
    public sealed class PathAndLinePackage : AsyncPackage
    {
        // Must match Identity Id in source.extension.vsixmanifest and guidPathAndLinePackage in VSCommandTable.vsct.
        public const string PackageGuidString = "29857012-cafc-4f0c-ae2a-2f7e87ca8d61";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Switch to UI thread before any COM or command registration.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Acquire DTE2 on the UI thread and pass to the command.
            var dte = (DTE2)GetService(typeof(DTE));

            await CopyPathAndLineCommand.InitializeAsync(this, dte);
        }
    }
}
