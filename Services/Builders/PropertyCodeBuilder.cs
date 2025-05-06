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
            bool isNullable = _typeResolver.IsNullableType(property);
            string nonNullableType = isNullable ? type.TrimEnd('?') : type;

            // Генерация getter
            string getter = isNullable
                ? $"get => _model?.{property.Name} ?? default({type});"
                : (_typeResolver.IsValueType(property)
                    ? $"get => _model?.{property.Name} ?? default({nonNullableType});"
                    : $"get => _model?.{property.Name};");

            // Генерация setter
            string comparison = isNullable
                ? $"!Nullable.Equals(_model.{property.Name}, value)"
                : (_typeResolver.IsValueType(property)
                    ? $"_model.{property.Name} != value"
                    : $"!EqualityComparer<{type}>.Default.Equals(_model.{property.Name}, value)");

            string assignment = isNullable
                ? $"_model.{property.Name} = value.HasValue ? ({nonNullableType})value : default({nonNullableType}?)"
                : $"_model.{property.Name} = value";

            return $@"
public {type} {property.Name}
{{
    {getter}
    set
    {{
        if (_model != null && {comparison})
        {{
            {assignment};
            OnPropertyChanged();
        }}
    }}
}}";
        }
    }
}