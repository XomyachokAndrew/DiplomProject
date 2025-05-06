using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class CommandCodeBuilder
    {
        public string BuildCommandCode(CodeFunction function)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string commandName = $"{function.Name}Command";

            return $@"
        public ICommand {commandName} {{ get; }}
    
        private void Execute{function.Name}(object parameter)
        {{
            _model?.{function.Name}({GetMethodParameters(function)});
        }}
    
        private bool CanExecute{function.Name}(object parameter)
        {{
            return _model != null;
        }}";
        }

        public string GenerateCommandInitializations(CodeClass modelClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var initializations = modelClass.Members
                .OfType<CodeFunction>()
                .Where(f =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return f.Access == vsCMAccess.vsCMAccessPublic;
                })
                .Select(f =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return $"{f.Name}Command = new RelayCommand(Execute{f.Name}, CanExecute{f.Name})";
                });

            return string.Join("\n            ", initializations);
        }

        private string GetMethodParameters(CodeFunction function)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var parameters = function.Parameters.OfType<CodeParameter>()
                .Select(p =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return $"({p.Type.AsString})parameter";
                });

            return function.Parameters.Count > 0
                ? string.Join(", ", parameters)
                : string.Empty;
        }
    }
}