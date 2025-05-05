using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class PropertyCodeBuilder
    {
        private readonly TypeResolver _typeResolver = new TypeResolver();

        public string BuildPropertiesCode(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string type = _typeResolver.GetPropertyType(property);

            return $@"
        public {type} {property.Name}
        {{
            get => _model.{property.Name};
            set
            {{
                if (!EqualityComparer<{type}>.Default.Equals(_model.{property.Name}, value))
                {{
                    _model.{property.Name} = value;
                    OnPropertyChanged();
                }}
            }}
        }}";
        }
    }
}
