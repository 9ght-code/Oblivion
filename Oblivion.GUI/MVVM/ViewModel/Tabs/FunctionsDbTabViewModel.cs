using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Oblivion.Data.Snapshots;
using Oblivion.GUI.Domain.Abstractions;
using Oblivion.GUI.MVVM.Model;

namespace Oblivion.GUI.MVVM.ViewModel.Tabs;

public partial class FunctionsDbTabViewModel : ViewModelBase
{
    public ObservableCollection<FunctionInfo> AllFunctions { get; } = new();

    [ObservableProperty]
    private ObservableCollection<FunctionInfo> _filteredFunctions = new();

    [ObservableProperty]
    private string _functionSearchText = "";

    [ObservableProperty]
    private string _severityFilter = "All";

    // Imports enriched with FunctionsDB severity info
    public ObservableCollection<ImportEntryViewModel> ImportEntriesEnriched { get; } = new();

    // Dangerous/Medium imported functions cross-referenced with FunctionsDB
    public ObservableCollection<FunctionInfo> DangerousImportedFunctions { get; } = new();

    public FunctionsDbTabViewModel(ObservableCollection<ImportSnapshot> importEntries)
    {
        LoadFunctionsDb();
        FilteredFunctions = new ObservableCollection<FunctionInfo>(AllFunctions);
        BuildImportSeverityMap(importEntries);
    }

    private void LoadFunctionsDb()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("WinApiFunctions.json"));

            if (resourceName == null) return;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return;

            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();

            var functions = JsonSerializer.Deserialize<List<FunctionInfo>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (functions != null)
            {
                foreach (var f in functions)
                    AllFunctions.Add(f);
            }
        }
        catch
        {
            // Functions DB not available
        }
    }

    private void BuildImportSeverityMap(ObservableCollection<ImportSnapshot> importEntries)
    {
        var lookup = new Dictionary<string, FunctionInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in AllFunctions)
            lookup[$"{f.Module}|{f.Name}"] = f;

        foreach (var imp in importEntries)
        {
            var enriched = new ImportEntryViewModel { ModuleName = imp.ModuleName };

            foreach (var funcName in imp.Functions)
            {
                lookup.TryGetValue($"{imp.ModuleName}|{funcName}", out var info);

                enriched.Functions.Add(new ImportFunctionEntry
                {
                    Name = funcName,
                    Severity = info?.Severity ?? "Safe",
                    Description = info?.Description ?? ""
                });

                if (info != null && (info.Severity == "Dangerous" || info.Severity == "Medium"))
                {
                    if (!DangerousImportedFunctions.Any(x => x.Module == imp.ModuleName && x.Name == funcName))
                        DangerousImportedFunctions.Add(info);
                }
            }

            ImportEntriesEnriched.Add(enriched);
        }
    }

    [RelayCommand]
    private void FilterFunctions()
    {
        var filtered = AllFunctions.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FunctionSearchText))
        {
            string search = FunctionSearchText.Trim().ToLowerInvariant();
            filtered = filtered.Where(f =>
                f.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                f.Module.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                f.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SeverityFilter != "All")
        {
            filtered = filtered.Where(f =>
                f.Severity.Equals(SeverityFilter, StringComparison.OrdinalIgnoreCase));
        }

        FilteredFunctions = new ObservableCollection<FunctionInfo>(filtered);
    }

    partial void OnFunctionSearchTextChanged(string value) => FilterFunctions();
    partial void OnSeverityFilterChanged(string value) => FilterFunctions();
}
