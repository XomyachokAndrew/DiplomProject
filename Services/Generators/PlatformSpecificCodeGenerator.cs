using System;
using DiplomProject.Enum;

namespace DiplomProject.Services.Generators
{
    public class PlatformSpecificCodeGenerator
    {
        private readonly PlatformType _platform;

        public PlatformSpecificCodeGenerator(PlatformType platform)
        {
            _platform = platform;
        }

        public string GetCommandType()
        {
            switch (_platform)
            {
                case PlatformType.WPF:
                    return "IRelayCommand";
                case PlatformType.UWP:
                    return "ICommand";
                case PlatformType.MAUI:
                    return "ICommand";
                default:
                    throw new NotImplementedException();
            }
        }

        public string GetCommandInitializationCode()
        {
            switch (_platform)
            {
                case PlatformType.WPF:
                    return $@"
            AddCommand = new RelayCommand(AddItem);
            EditCommand = new RelayCommand(EditItem, () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteItem, () => SelectedItem != null);
            SaveCommand = new RelayCommand(SaveChanges);";
                case PlatformType.UWP:
                    return $@"
            AddCommand = new RelayCommand(AddItem);
            EditCommand = new RelayCommand(EditItem, () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteItem, () => SelectedItem != null);
            SaveCommand = new RelayCommand(SaveChanges);";
                case PlatformType.MAUI:
                    return $@"
            AddCommand = new Command(AddItem);
            EditCommand = new Command(EditItem, () => SelectedItem != null);
            DeleteCommand = new Command(DeleteItem, () => SelectedItem != null);
            SaveCommand = new Command(SaveChanges);";
                default:
                    throw new NotImplementedException();
            }
        }

        public string GetNotifyCommandChangedCode()
        {
            switch (_platform)
            {
                case PlatformType.WPF:
                    return @"EditCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();";
                case PlatformType.UWP:
                    return @"(EditCommand as RelayCommand)?.NotifyCanExecuteChanged();
                (DeleteCommand as RelayCommand)?.NotifyCanExecuteChanged();";
                case PlatformType.MAUI:
                    return @"(EditCommand as Command)?.ChangeCanExecute();
                (DeleteCommand as Command)?.ChangeCanExecute();";
                default:
                    throw new NotImplementedException();
            }
        }

        public string GetAdditionalUsings()
        {
            switch (_platform)
            {
                case PlatformType.WPF:
                    return @"using CommunityToolkit.Mvvm.Input;
using System.Windows;";
                case PlatformType.UWP:
                    return @"using Windows.UI.Xaml.Input;
using Windows.UI.Popups;";
                case PlatformType.MAUI:
                    return @"using Microsoft.Maui.Controls;";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
