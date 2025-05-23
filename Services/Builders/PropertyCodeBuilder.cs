using System.Text;
using DiplomProject.Enum;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class PropertyCodeBuilder
    {
        private readonly PlatformType _platform;
        private readonly TypeResolver _typeResolver;

        public PropertyCodeBuilder(PlatformType platform)
        {
            _platform = platform;
            _typeResolver = new TypeResolver();
        }

        public string BuildPropertyCode(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string type = _typeResolver.GetPropertyType(property);
            string fieldName = GetFieldName(property.Name);

            var sb = new StringBuilder();

            sb.AppendLine($@"
        private {type} {fieldName};");

            if (_platform == PlatformType.WPF || _platform == PlatformType.MAUI)
            {
                sb.AppendLine($@"        [ObservableProperty]
        private {type} {fieldName};");
            }
            else
            {
                sb.AppendLine($@"
        public {type} {property.Name}
        {{
            get => {fieldName};
            set
            {{
                if ({fieldName} != value)
                {{
                    {fieldName} = value;
                    OnPropertyChanged(nameof({property.Name}));
                }}
            }}
        }}");
            }

            return sb.ToString();
        }

        public string BuildPropertyDialogCode(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string type = _typeResolver.GetPropertyType(property);

            var sb = new StringBuilder();

            if (_platform == PlatformType.WPF || _platform == PlatformType.MAUI)
            {
                sb.AppendLine($@"private {type} {GetFieldName(property.Name)};");
            }
            else
            {
                sb.AppendLine($@"
        public {type} {property.Name}
        {{
            get => _item.{property.Name};
            set
            {{
                if (_item.{property.Name} != value)
                {{
                    _item.{property.Name} = value;
                    OnPropertyChanged(nameof({property.Name}));
                }}
            }}
        }}");
            }

            return sb.ToString();
        }

        private string GetFieldName(string propertyName)
        {
            switch (_platform)
            {
                case PlatformType.WPF:
                    return $"_{propertyName.ToLower()}";
                case PlatformType.UWP:
                    return $"_{propertyName.ToLower()}";
                case PlatformType.MAUI:
                    return $"m_{propertyName}";
                default:
                    return $"_{propertyName}";
            }
        }

        public string BuildInitPropertyCode(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return $"{property.Name} = item.{property.Name};";
        }

        public string BuildPropertyAssignment(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return $"{property.Name} = {property.Name},";
        }
    }
}