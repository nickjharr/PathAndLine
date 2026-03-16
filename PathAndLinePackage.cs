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
        // Must match Identity Id in source.extension.vsixmanifest and guidPathAndLinePackage in VSCommandTable.vsct.
        public const string PackageGuidString = "39cfcaed-cfa9-4d6e-8c63-8e48281dba00";

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
