using System;
using System.ComponentModel.Design;
using System.Windows;
using DiplomProject.Controls;
using DiplomProject.Services;
using DiplomProject.Services.Detectors;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace DiplomProject
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GenerateXAML
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("6cd20ec3-b75b-4028-937d-0f4dcadcfa4e");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateXAML"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private GenerateXAML(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenerateXAML Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new GenerateXAML(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string title = "Генерация Xaml";

            // Получаем текущий проект
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            Project activeProject = GetActiveProject(dte);

            // Определяем платформу
            var platform = ProjectPlatformDetector.Detect(activeProject);

            var dialog = new System.Windows.Window
            {
                Title = title,
                Width = 400,
                Height = 500,
                Content = new SelectionControl(),
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            if (dialog.Content is SelectionControl control)
            {
                control.OkClicked += (s, args) =>
                {
                    try
                    {
                        var xamlGenerator = new XamlGeneratorService(package, platform);

                        xamlGenerator.GenerateXaml(
                            className: control.SelectedModel,
                            isGenerateViewModel: control.GenerateViewModel,
                            isUseDataBinding: control.UseDataBinding,
                            isUseDatabase: control.UseDatabase,
                            dbProvider: control.SelectedDbProvider,
                            isAddingMethod: control.AddAddingMethod,
                            isEditingMethod: control.AddEditingMethod,
                            isDeletingMethod: control.AddDeletingMethod,
                            isDialog: control.AddDialog
                            );

                        // Показываем сообщение об успехе
                        VsShellUtilities.ShowMessageBox(
                            package,
                            "XAML generation completed successfully!",
                            title,
                            OLEMSGICON.OLEMSGICON_INFO,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    catch (Exception ex)
                    {
                        // Обработка ошибок
                        VsShellUtilities.ShowMessageBox(
                            package,
                            $"Error during generation: {ex.Message}",
                            title,
                            OLEMSGICON.OLEMSGICON_CRITICAL,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    finally
                    {
                        dialog.Close();
                    }
                    
                };

                control.CancelClicked += (s, args) => dialog.Close();
            }

            dialog.ShowDialog();
        }

        private Project GetActiveProject(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                Array activeProjects = (Array)dte.ActiveSolutionProjects;
                return activeProjects.Length > 0 ? (Project)activeProjects.GetValue(0) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
