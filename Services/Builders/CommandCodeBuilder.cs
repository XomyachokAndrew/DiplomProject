using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class CommandCodeBuilder
    {
        private readonly DataSourceResolver _dataSourceResolver = new DataSourceResolver();

        public string BuildCommandCode(CodeFunction function)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string commandName = $"{function.Name}Command";

            return $@"
        public ICommand {commandName} {{ get; }}
    
        private void Execute{function.Name}()
        {{
            _items?.{function.Name}({GetMethodParameters(function)});
        }}
    
        private bool CanExecute{function.Name}()
        {{
            return _items != null;
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
                    Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                    return $"{f.Name}Command = new RelayCommand(Execute{f.Name}, CanExecute{f.Name})";
                });

            return string.Join("\n            ", initializations);
        }

        private string GetMethodParameters(CodeFunction function)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var parameters = new List<string>();
            foreach (CodeParameter p in function.Parameters)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                parameters.Add($"default({p.Type.AsString})");
            }

            return string.Join(", ", parameters);
        }
    }
}