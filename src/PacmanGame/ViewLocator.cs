using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using PacmanGame.ViewModels;

namespace PacmanGame;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        var viewModelType = param.GetType();
        var baseName = viewModelType.Name.Replace("ViewModel", "View", StringComparison.Ordinal);
        var candidates = new List<string>();
        candidates.Add(viewModelType.FullName!.Replace("ViewModel", "View", StringComparison.Ordinal));

        if (!string.IsNullOrEmpty(viewModelType.Namespace))
        {
            var namespaceCandidate = viewModelType.Namespace.Replace("ViewModels", "Views", StringComparison.Ordinal);
            candidates.Add($"{namespaceCandidate}.{baseName}");
        }

        candidates.Add($"PacmanGame.Views.{baseName}");

        var tried = new HashSet<string>();
        foreach (var candidate in candidates)
        {
            if (!tried.Add(candidate))
            {
                continue;
            }

            var type = Type.GetType(candidate);
            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }
        }

        return new TextBlock { Text = "Not Found: " + string.Join(", ", candidates) };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
