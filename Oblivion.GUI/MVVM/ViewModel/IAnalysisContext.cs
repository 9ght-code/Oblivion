using System;
using System.Collections.ObjectModel;
using Oblivion.Data.Snapshots;

namespace Oblivion.GUI.MVVM.ViewModel;

public interface IAnalysisContext
{
    string FilePath { get; }
    string Architecture { get; }
    byte[]? FileBytes { get; set; }
    PESnapshot? Snapshot { get; }
    ObservableCollection<PESectionSnapshot> Sections { get; }
    ObservableCollection<ImportSnapshot> ImportEntries { get; }
    string EntryPoint { get; set; }
    int EntryPointRaw { get; set; }
    bool IsModified { get; set; }
    int ActiveTabIndex { get; set; }
    string EditStatus { get; set; }
    event Action? HexStreamRefreshRequested;

    void NotifyFileModified();
    void RefreshAllViews();
    void SwitchToHexAtOffset(long offset);
    void SwitchToDisasmAtRva(int rva);
    long? HexGoToTarget { get; set; }
    string ShellcodeHex { get; set; }
}
