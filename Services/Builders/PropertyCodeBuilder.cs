using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class PropertyCodeBuilder
    {
        private readonly TypeResolver _typeResolver = new TypeResolver();

        public string BuildPropertyCode(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string type = _typeResolver.GetPropertyType(property);

            return $@"
        private {type} _{property.Name.ToLower()};
        public {type} {property.Name}
        {{
            get => _{property.Name.ToLower()};
            set
            {{
                _{property.Name.ToLower()} = value;
                OnPropertyChanged(nameof({property.Name}));
            }}
        }}
";
        }

        public string BuildPropertyDialogCode(CodeProperty property, CodeClass codeClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string type = _typeResolver.GetPropertyType(property);

            return $@"
        public {type} {property.Name}
        {{
            get => _item.{property.Name};
            set
            {{
                _item.{property.Name} = value;
                OnPropertyChanged(nameof({property.Name}));
            }}
        }}
";
        }
    }
}