using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using DiplomProject.Services.Builder;

namespace DiplomProject.Services
{
    public class MessageService
    {
        private readonly AsyncPackage _package;

        public MessageService(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public void ShowSuccessMessage(string message)
        {
            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                "Success",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public void ShowErrorMessage(string message)
        {
            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                "Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
