using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomProject.Enum;
using EnvDTE;
using EnvDTE80;

namespace DiplomProject.Services.Handler
{
    public class DialogHandler
    {
        private readonly PlatformType _platform;
        private readonly CodeClass _codeClass;

        public DialogHandler(PlatformType platform, CodeClass codeClass)
        {
            _platform = platform;
            _codeClass = codeClass;
        }

        public string GetEditDialogCode(bool isDialog)
        {
            if (!isDialog) return GetSimpleEditCode();

            switch (_platform)
            {
                case PlatformType.WPF:
                    return GetWpfEditDialogCode();
                case PlatformType.UWP:
                    return GetUwpEditDialogCode();
                case PlatformType.MAUI:
                    return GetMauiEditDialogCode();
                default:
                    return GetSimpleEditCode();
            }
        }

        public string GetAddDialogCode(bool isDialog)
        {
            if (!isDialog) return GetSimpleAddCode();

            switch (_platform)
            {
                case PlatformType.WPF:
                    return GetWpfAddDialogCode();
                case PlatformType.UWP:
                    return GetUwpAddDialogCode();
                case PlatformType.MAUI:
                    return GetMauiAddDialogCode();
                default:
                    return GetSimpleAddCode();
            }
        }

        public string GetDeleteConfirmationCode()
        {

            switch (_platform)
            {
                case PlatformType.WPF:
                    return GetWpfDeleteConfirmationCode();
                case PlatformType.UWP:
                    return GetUwpDeleteConfirmationCode();
                case PlatformType.MAUI:
                    return GetMauiDeleteConfirmationCode();
                default:
                    return GetSimpleDeleteCode();
            }
        }

        #region Edit Dialog Implementations
        private string GetSimpleEditCode()
        {
            return $@"
            if (SelectedItem != null)
            {{
                var updatedItem = SelectedItem;
                SaveChanges();
                OnPropertyChanged(nameof(Items));
            }}";
        }

        private string GetWpfEditDialogCode()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return $@"
            if (SelectedItem != null)
            {{
                var itemCopy = new {_codeClass.Name}
                {{
                    // Копируем все свойства
                    {GeneratePropertyCopies("SelectedItem")}
                }};

                var dialogVm = new Dialog{_codeClass.Name}ViewModel(itemCopy, ""Edit {_codeClass.Name}"");
                var dialog = new Dialog{_codeClass.Name}View
                {{
                    Owner = Application.Current.MainWindow,
                    DataContext = dialogVm
                }};

                if (dialog.ShowDialog() == true && dialogVm.IsSaved)
                {{
                    {GeneratePropertyUpdates("dialogVm.GetItem()", "SelectedItem")}
                    SaveChanges();
                }}
                OnPropertyChanged(nameof(Items));
            }}";
        }

        private string GetMauiEditDialogCode()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return $@"
            if (SelectedItem != null)
            {{
                var itemCopy = new {_codeClass.Name}
                {{
                    {GeneratePropertyCopies("SelectedItem")}
                }};

                var editPage = new Dialog{_codeClass.Name}View
                {{
                    BindingContext = new Dialog{_codeClass.Name}ViewModel(itemCopy)
                }};

                await Shell.Current.Navigation.PushModalAsync(editPage);
            
                if (editPage.BindingContext is Dialog{_codeClass.Name}ViewModel vm && vm.IsSaved)
                {{
                    {GeneratePropertyUpdates("vm.GetItem()", "SelectedItem")}
                    SaveChanges();
                }}
                OnPropertyChanged(nameof(Items));
            }}";
        }

        private string GetUwpEditDialogCode()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return $@"
            if (SelectedItem != null)
            {{
                var itemCopy = new {_codeClass.Name}
                {{
                    {GeneratePropertyCopies("SelectedItem")}
                }};

                var dialog = new ContentDialog
                {{
                    Title = ""Edit {_codeClass.Name}"",
                    Content = new Dialog{_codeClass.Name}View(),
                    PrimaryButtonText = ""Save"",
                    SecondaryButtonText = ""Cancel""
                }};

                var vm = new Dialog{_codeClass.Name}ViewModel(itemCopy);
                dialog.DataContext = vm;

                if (await dialog.ShowAsync() == ContentDialogResult.Primary && vm.IsSaved)
                {{
                    {GeneratePropertyUpdates("vm.GetItem()", "SelectedItem")}
                    SaveChanges();
                }}
                OnPropertyChanged(nameof(Items));
            }}";
        }
        #endregion

        #region Add Dialog Implementations
        private string GetSimpleAddCode()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return $@"
            var newItem = new {_codeClass.Name}();
            newItem.Id = Items.Any() ? Items.Max(p => p.Id) + 1 : 1;
            Items.Add(newItem);
            SaveChanges();";
        }

        private string GetWpfAddDialogCode()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return $@"
            var dialogVm = new Dialog{_codeClass.Name}ViewModel(new {_codeClass.Name}(), ""Add New {_codeClass.Name}"");
            var dialog = new Dialog{_codeClass.Name}View
            {{
                Owner = Application.Current.MainWindow,
                DataContext = dialogVm
            }};

            if (dialog.ShowDialog() == true && dialogVm.IsSaved)
            {{
                var newItem = dialogVm.GetItem();
                newItem.Id = Items.Any() ? Items.Max(p => p.Id) + 1 : 1;
                Items.Add(newItem);
                SaveChanges();
            }}";
        }

        private string GetMauiAddDialogCode()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return $@"
            var addPage = new Dialog{_codeClass.Name}View
            {{
                BindingContext = new Dialog{_codeClass.Name}ViewModel(new {_codeClass.Name}())
            }};

            await Shell.Current.Navigation.PushModalAsync(addPage);
        
            if (addPage.BindingContext is Dialog{_codeClass.Name}ViewModel vm && vm.IsSaved)
            {{
                var newItem = vm.GetItem();
                newItem.Id = Items.Any() ? Items.Max(p => p.Id) + 1 : 1;
                Items.Add(newItem);
                SaveChanges();
            }}";
        }

        private string GetUwpAddDialogCode()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return $@"
            var dialog = new ContentDialog
            {{
                Title = ""Add New {_codeClass.Name}"",
                Content = new Dialog{_codeClass.Name}View(),
                PrimaryButtonText = ""Add"",
                SecondaryButtonText = ""Cancel""
            }};

            var vm = new Dialog{_codeClass.Name}ViewModel(new {_codeClass.Name}());
            dialog.DataContext = vm;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary && vm.IsSaved)
            {{
                var newItem = vm.GetItem();
                newItem.Id = Items.Any() ? Items.Max(p => p.Id) + 1 : 1;
                Items.Add(newItem);
                SaveChanges();
            }}";
        }
        #endregion

        #region Delete Confirmation Implementations
        private string GetSimpleDeleteCode()
        {
            return $@"
            if (SelectedItem != null)
            {{
                Items.Remove(SelectedItem);
                SaveChanges();
            }}";
        }

        private string GetWpfDeleteConfirmationCode()
        {
            return $@"
            if (SelectedItem != null && 
                MessageBox.Show(""Delete this item?"", ""Confirm"", 
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {{
                Items.Remove(SelectedItem);
                SaveChanges();
            }}";
        }

        private string GetMauiDeleteConfirmationCode()
        {
            return $@"
            if (SelectedItem != null && 
                await Shell.Current.DisplayAlert(""Confirm"", 
                ""Delete this item?"", ""Yes"", ""No""))
            {{
                Items.Remove(SelectedItem);
                SaveChanges();
            }}";
        }

        private string GetUwpDeleteConfirmationCode()
        {
            return $@"
            if (SelectedItem != null)
            {{
                var dialog = new MessageDialog(""Delete this item?"", ""Confirm"");
                dialog.Commands.Add(new UICommand(""Yes""));
                dialog.Commands.Add(new UICommand(""No""));
            
                if (await dialog.ShowAsync()?.Label == ""Yes"")
                {{
                    Items.Remove(SelectedItem);
                    SaveChanges();
                }}
            }}";
        }
        #endregion

        #region Helpers
        private StringBuilder GeneratePropertyCopies(string sourceObject)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var propertyCopies = new StringBuilder();

            foreach (CodeElement member in _codeClass.Members)
            {
                if (member is CodeProperty property)
                {
                    propertyCopies.AppendLine($@"{property.Name} = {sourceObject}.{property.Name},");
                }
            }
            // В реальной реализации нужно анализировать свойства класса
            // Здесь упрощенный вариант для примера
            return propertyCopies;
        }

        private StringBuilder GeneratePropertyUpdates(string sourceObject, string targetObject)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var propertyUpdates = new StringBuilder();

            foreach (CodeElement member in _codeClass.Members)
            {
                if (member is CodeProperty property)
                {
                    propertyUpdates.AppendLine($@"{targetObject}.{property.Name} = {sourceObject}.{property.Name};");
                }
            }

            return propertyUpdates;
        }
        #endregion
    }
}
