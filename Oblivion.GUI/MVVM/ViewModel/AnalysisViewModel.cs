using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Oblivion.Data.Snapshots;
using Oblivion.GUI.Domain.Abstractions;
using Oblivion.GUI.MVVM.Model;
using Oblivion.GUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Oblivion.GUI.MVVM.ViewModel
{
    public partial class AnalysisViewModel : ViewModelBase, IAnalysisContext
    {
        // File info
        public string FileName { get; }
        public string FilePath { get; }
        public string Architecture { get; }
        public string FileSize { get; }
        public string Hash { get; }

        [ObservableProperty]
        private string _entryPoint = "N/A";

        public int EntryPointRaw { get; set; }

        // Snapshot data
        [ObservableProperty]
        private PESnapshot? _snapshot;

        // Overview
        public string Machine { get; }
        public string TimeDateStamp { get; }
        public string Characteristics { get; }
        public string Subsystem { get; }
        public string DllCharacteristics { get; }
        public string ImageBase { get; }

        // Sections
        public ObservableCollection<PESectionSnapshot> Sections { get; } = new();

        // Imports (full tree: module -> functions)
        public ObservableCollection<ImportSnapshot> ImportEntries { get; } = new();

        // Proxy to FunctionsDbTab collections (used by ImportsTab and SecurityTab)
        public ObservableCollection<ImportEntryViewModel> ImportEntriesEnriched => FunctionsDbTab?.ImportEntriesEnriched ?? [];
        public ObservableCollection<FunctionInfo> DangerousImportedFunctions => FunctionsDbTab?.DangerousImportedFunctions ?? [];

        // Hex data
        [ObservableProperty]
        private byte[]? _fileBytes;

        [ObservableProperty]
        private string _hexOffset = "0";

        [ObservableProperty]
        private long? _hexGoToTarget;

        // Disassembly
        public ObservableCollection<DisassembledInstruction> DisassembledInstructions { get; } = new();

        [ObservableProperty]
        private string _disasmGoToAddress = "";

        [ObservableProperty]
        private string _disasmStatus = "";

        // Tab ViewModels
        [ObservableProperty]
        private Tabs.StringsTabViewModel? _stringsTab;

        [ObservableProperty]
        private Tabs.FunctionsDbTabViewModel? _functionsDbTab;

        // Security flags
        public bool HasAslr { get; }
        public bool HasDep { get; }
        public bool HasSeh { get; }
        public bool HasCfg { get; }
        public List<SecurityFlag> SecurityFlags { get; } = new();

        // Entry point editor
        [ObservableProperty]
        private string _newEntryPoint = "";

        [ObservableProperty]
        private string _entryPointSection = "";

        // Loading state
        [ObservableProperty]
        private bool _isLoading = true;

        // Editing state
        [ObservableProperty]
        private bool _isModified;

        [ObservableProperty]
        private int _activeTabIndex;

        [ObservableProperty]
        private string _editStatus = "";

        // Shellcode tab
        [ObservableProperty]
        private string _shellcodeHex = "";

        [ObservableProperty]
        private string _shellcodeSectionName = ".obli";

        [ObservableProperty]
        private bool _shellcodeRedirectEp = true;

        [ObservableProperty]
        private string _shellcodeInjectionMode = "NewSection";

        [ObservableProperty]
        private string _shellcodeStatus = "";

        // Disasm assembler
        [ObservableProperty]
        private string _assembleInput = "";

        [ObservableProperty]
        private string _assembleTargetAddress = "";

        [ObservableProperty]
        private string _patchBytesInput = "";

        [ObservableProperty]
        private Tabs.ToolsTabViewModel? _toolsTab;

        // Event to notify views of hex stream refresh
        public event Action? HexStreamRefreshRequested;

        // Track original file for revert
        private byte[]? _originalFileBytes;

        public AnalysisViewModel(FileUIModel file, PESnapshot? snapshot)
        {
            FileName = file.Name;
            FilePath = file.Path;
            Architecture = file.Architecture;
            Hash = file.Hash;
            EntryPoint = file.EntryPoint ?? "N/A";
            Snapshot = snapshot;

            FileSize = FormatFileSize(file.FileSize);
            EntryPointRaw = snapshot?.EntryPoint ?? 0;
            NewEntryPoint = EntryPoint;

            // Header info
            if (snapshot?.Header != null)
            {
                Machine = FormatMachine(snapshot.Header.Machine);
                TimeDateStamp = FormatTimestamp(snapshot.Header.TimeDateStamp);
                Characteristics = $"0x{snapshot.Header.Characteristics:X4}";
                Subsystem = FormatSubsystem(snapshot.Header.Subsystem);
                DllCharacteristics = $"0x{(snapshot.Header.DllCharacteristics ?? 0):X4}";

                ushort dllChar = snapshot.Header.DllCharacteristics ?? 0;
                HasAslr = (dllChar & 0x0040) != 0;
                HasDep = (dllChar & 0x0100) != 0;
                HasSeh = (dllChar & 0x0400) == 0;
                HasCfg = (dllChar & 0x4000) != 0;

                SecurityFlags.AddRange(new[]
                {
                    new SecurityFlag { Title = "ASLR (Address Space Layout Randomization)", Description = "IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE (0x0040)", Icon = Wpf.Ui.Controls.SymbolRegular.Shield24, IsEnabled = HasAslr },
                    new SecurityFlag { Title = "DEP / NX (Data Execution Prevention)", Description = "IMAGE_DLLCHARACTERISTICS_NX_COMPAT (0x0100)", Icon = Wpf.Ui.Controls.SymbolRegular.ShieldTask24, IsEnabled = HasDep },
                    new SecurityFlag { Title = "SEH (Structured Exception Handling)", Description = "IMAGE_DLLCHARACTERISTICS_NO_SEH absent (0x0400)", Icon = Wpf.Ui.Controls.SymbolRegular.ShieldError24, IsEnabled = HasSeh, IsWarning = true },
                    new SecurityFlag { Title = "CFG (Control Flow Guard)", Description = "IMAGE_DLLCHARACTERISTICS_GUARD_CF (0x4000)", Icon = Wpf.Ui.Controls.SymbolRegular.ShieldKeyhole24, IsEnabled = HasCfg, IsWarning = true },
                });
            }
            else
            {
                Machine = "N/A";
                TimeDateStamp = "N/A";
                Characteristics = "N/A";
                Subsystem = "N/A";
                DllCharacteristics = "N/A";
            }

            ImageBase = snapshot != null ? $"0x{snapshot.ImageBase:X}" : "N/A";

            // Sections
            if (snapshot?.Sections != null)
                foreach (var s in snapshot.Sections) Sections.Add(s);

            // Imports
            if (snapshot?.Imports != null)
                foreach (var i in snapshot.Imports) ImportEntries.Add(i);

            // Resolve EP section (lightweight)
            ResolveEntryPointSection(EntryPointRaw);

            // Kick off heavy initialization asynchronously
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Read file + disassemble on thread pool
                byte[]? bytes = null;
                if (File.Exists(FilePath))
                    bytes = await Task.Run(() => File.ReadAllBytes(FilePath));

                if (bytes != null)
                {
                    FileBytes = bytes;
                    _originalFileBytes = (byte[])bytes.Clone();
                }

                // Build sub-VMs on thread pool (CPU-bound work)
                var stringsTab = await Task.Run(() => new Tabs.StringsTabViewModel(this));
                var functionsDbTab = await Task.Run(() => new Tabs.FunctionsDbTabViewModel(ImportEntries));
                var toolsTab = new Tabs.ToolsTabViewModel(this, ImageBase);

                // Assign on UI thread (ObservableProperty raises change notifications)
                StringsTab = stringsTab;
                FunctionsDbTab = functionsDbTab;
                ToolsTab = toolsTab;

                OnPropertyChanged(nameof(ImportEntriesEnriched));
                OnPropertyChanged(nameof(DangerousImportedFunctions));

                // Run initial disassembly
                if (FileBytes != null)
                    RunDisassembly(EntryPointRaw);
            }
            finally
            {
                IsLoading = false;
            }
        }


        #region Hex Editor

        [RelayCommand]
        private void GoToOffset()
        {
            if (string.IsNullOrWhiteSpace(HexOffset)) return;

            string cleaned = HexOffset.Trim();
            if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned[2..];

            if (long.TryParse(cleaned, System.Globalization.NumberStyles.HexNumber, null, out long offset))
            {
                HexGoToTarget = offset;
            }
        }

        [RelayCommand]
        private void SaveFile()
        {
            if (FileBytes == null) return;

            try
            {
                PEWriterService.RecalculateChecksum(FileBytes);
                PEWriterService.SaveFile(FileBytes, FilePath);
                _originalFileBytes = (byte[])FileBytes.Clone();
                IsModified = false;
                EditStatus = "File saved successfully.";
            }
            catch (Exception ex)
            {
                EditStatus = $"Save failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SaveFileAs(string? path)
        {
            if (FileBytes == null || string.IsNullOrEmpty(path)) return;

            try
            {
                PEWriterService.RecalculateChecksum(FileBytes);
                PEWriterService.SaveFile(FileBytes, path);
                EditStatus = $"Saved to: {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                EditStatus = $"Save As failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void RevertFile()
        {
            if (_originalFileBytes == null) return;

            FileBytes = (byte[])_originalFileBytes.Clone();
            IsModified = false;
            RefreshAllViews();
            EditStatus = "Reverted to original.";
        }

        public void NotifyFileModified()
        {
            IsModified = true;
        }

        #endregion

        #region Entry Point Editor

        [RelayCommand]
        private void ChangeEntryPoint()
        {
            if (FileBytes == null || string.IsNullOrWhiteSpace(NewEntryPoint)) return;

            try
            {
                string cleaned = NewEntryPoint.Trim();
                if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    cleaned = cleaned[2..];

                if (!uint.TryParse(cleaned, System.Globalization.NumberStyles.HexNumber, null, out uint newRva))
                {
                    EditStatus = "Invalid hex address.";
                    return;
                }

                PEWriterService.ChangeEntryPoint(FileBytes, newRva);
                EntryPointRaw = (int)newRva;
                EntryPoint = $"0x{newRva:X}";
                IsModified = true;
                ResolveEntryPointSection((int)newRva);
                EditStatus = $"Entry point changed to 0x{newRva:X}.";
            }
            catch (Exception ex)
            {
                EditStatus = $"Change EP failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void InjectShellcodeAtEp()
        {
            if (FileBytes == null || string.IsNullOrWhiteSpace(ShellcodeHex)) return;

            try
            {
                byte[] shellcode = DisassemblyService.ParseHexBytes(ShellcodeHex);
                if (shellcode.Length == 0)
                {
                    EditStatus = "No shellcode bytes.";
                    return;
                }

                int bitness = Architecture.Contains("64") ? 64 : 32;
                byte[] newFile = PEWriterService.InjectShellcodeWithEpRedirect(
                    FileBytes, shellcode, ".shell", true, bitness);

                FileBytes = newFile;
                _originalFileBytes = null; // can't revert after structural change
                IsModified = true;

                RefreshAllViews();
                EditStatus = "Shellcode injected at EP with redirect.";
            }
            catch (Exception ex)
            {
                EditStatus = $"Inject at EP failed: {ex.Message}";
            }
        }

        #endregion

        #region Sections Editor

        [RelayCommand]
        private void AddNewSection(object? parameter)
        {
            // Called from dialog with tuple (name, data, characteristics)
            if (FileBytes == null || parameter is not (string name, byte[] data, uint characteristics))
                return;

            try
            {
                byte[] newFile = PEWriterService.AddSection(FileBytes, name, data, characteristics);
                FileBytes = newFile;
                _originalFileBytes = null;
                IsModified = true;
                RefreshAllViews();
                EditStatus = $"Section '{name}' added.";
            }
            catch (Exception ex)
            {
                EditStatus = $"Add section failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void EditSectionCharacteristics(object? parameter)
        {
            if (FileBytes == null || parameter is not (int index, uint newChars))
                return;

            try
            {
                PEWriterService.ModifySectionProperties(FileBytes, index, newCharacteristics: newChars);
                IsModified = true;
                RefreshAllViews();
                EditStatus = $"Section characteristics updated.";
            }
            catch (Exception ex)
            {
                EditStatus = $"Edit section failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ViewSectionInHex(PESectionSnapshot? section)
        {
            if (section == null) return;

            ActiveTabIndex = 2; // Hex Editor tab index
            HexGoToTarget = section.RawDataPointer;
        }

        [RelayCommand]
        private void ViewSectionInDisasm(PESectionSnapshot? section)
        {
            if (section == null || FileBytes == null) return;

            ActiveTabIndex = 5; // Disassembler tab index
            int rva = (int)section.VirtualAddress;
            RunDisassembly(rva);
            DisasmGoToAddress = $"0x{section.VirtualAddress:X}";
        }

        #endregion

        #region Disassembler Editor

        [RelayCommand]
        private void DisassembleFromAddress()
        {
            if (string.IsNullOrWhiteSpace(DisasmGoToAddress)) return;

            string cleaned = DisasmGoToAddress.Trim();
            if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned[2..];

            if (long.TryParse(cleaned, System.Globalization.NumberStyles.HexNumber, null, out long addr))
            {
                long ib = Snapshot?.ImageBase ?? 0;
                int rva = (ib > 0 && addr >= ib) ? (int)(addr - ib) : (int)addr;
                RunDisassembly(rva);
            }
        }

        [RelayCommand]
        private void NopInstruction(DisassembledInstruction? instr)
        {
            if (instr == null || FileBytes == null) return;

            try
            {
                DisassemblyService.NopInstruction(FileBytes, instr.FileOffset, instr.Length);
                IsModified = true;

                // Re-run disassembly from current location
                long ib = Snapshot?.ImageBase ?? 0;
                int rva = (int)(instr.RawAddress - (ulong)ib);
                int currentRva = DisassembledInstructions.Count > 0
                    ? (int)(DisassembledInstructions[0].RawAddress - (ulong)ib)
                    : rva;
                RunDisassembly(currentRva);
                HexStreamRefreshRequested?.Invoke();
                EditStatus = $"NOPed {instr.Length} bytes at {instr.Address}.";
            }
            catch (Exception ex)
            {
                EditStatus = $"NOP failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void PatchInstructionBytes(DisassembledInstruction? instr)
        {
            if (instr == null || FileBytes == null || string.IsNullOrWhiteSpace(PatchBytesInput)) return;

            try
            {
                byte[] newBytes = DisassemblyService.ParseHexBytes(PatchBytesInput);
                DisassemblyService.PatchInstruction(FileBytes, instr.FileOffset, instr.Length, newBytes);
                IsModified = true;

                long ib = Snapshot?.ImageBase ?? 0;
                int currentRva = DisassembledInstructions.Count > 0
                    ? (int)(DisassembledInstructions[0].RawAddress - (ulong)ib)
                    : 0;
                RunDisassembly(currentRva);
                HexStreamRefreshRequested?.Invoke();
                EditStatus = $"Patched {newBytes.Length} bytes at {instr.Address}.";
            }
            catch (Exception ex)
            {
                EditStatus = $"Patch failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void AssembleAndPatch()
        {
            if (FileBytes == null || string.IsNullOrWhiteSpace(AssembleInput)
                || string.IsNullOrWhiteSpace(AssembleTargetAddress)) return;

            try
            {
                string cleaned = AssembleTargetAddress.Trim();
                if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    cleaned = cleaned[2..];

                if (!ulong.TryParse(cleaned, System.Globalization.NumberStyles.HexNumber, null, out ulong targetAddr))
                {
                    EditStatus = "Invalid target address.";
                    return;
                }

                int bitness = Architecture.Contains("64") ? 64 : 32;
                byte[] assembled = DisassemblyService.AssembleInstruction(AssembleInput, bitness, targetAddr);

                if (assembled.Length == 0)
                {
                    EditStatus = "Assembly produced no bytes.";
                    return;
                }

                // Convert address to file offset
                long ib = Snapshot?.ImageBase ?? 0;
                int rva = (int)(targetAddr - (ulong)ib);
                long fileOffset = DisassemblyService.RvaToFileOffset(rva, Snapshot?.Sections ?? new List<PESectionSnapshot>());

                if (fileOffset < 0 || fileOffset + assembled.Length > FileBytes.Length)
                {
                    EditStatus = "Target address is outside file bounds.";
                    return;
                }

                PEWriterService.PatchBytes(FileBytes, fileOffset, assembled);
                IsModified = true;

                int currentRva = DisassembledInstructions.Count > 0
                    ? (int)(DisassembledInstructions[0].RawAddress - (ulong)ib)
                    : rva;
                RunDisassembly(currentRva);
                HexStreamRefreshRequested?.Invoke();
                EditStatus = $"Assembled & patched {assembled.Length} bytes at 0x{targetAddr:X}.";
            }
            catch (Exception ex)
            {
                EditStatus = $"Assemble failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ViewInHex(DisassembledInstruction? instr)
        {
            if (instr == null) return;
            ActiveTabIndex = 2; // Hex Editor tab
            HexGoToTarget = instr.FileOffset;
        }

        #endregion

        #region Shellcode Injector

        [RelayCommand]
        private void InjectShellcode()
        {
            if (FileBytes == null || string.IsNullOrWhiteSpace(ShellcodeHex)) return;

            try
            {
                byte[] shellcode = DisassemblyService.ParseHexBytes(ShellcodeHex);
                if (shellcode.Length == 0)
                {
                    ShellcodeStatus = "No shellcode bytes provided.";
                    return;
                }

                int bitness = Architecture.Contains("64") ? 64 : 32;

                switch (ShellcodeInjectionMode)
                {
                    case "NewSection":
                    {
                        if (ShellcodeRedirectEp)
                        {
                            FileBytes = PEWriterService.InjectShellcodeWithEpRedirect(
                                FileBytes, shellcode, ShellcodeSectionName, true, bitness);
                        }
                        else
                        {
                            var (newFile, rva) = PEWriterService.InjectShellcode(
                                FileBytes, shellcode, ShellcodeSectionName);
                            FileBytes = newFile;
                        }
                        break;
                    }
                    case "CodeCave":
                    {
                        int peOffset = PEWriterService.FindPEHeaderOffset(FileBytes);
                        var caves = PEWriterService.FindCodeCaves(FileBytes, shellcode.Length, peOffset);

                        if (caves.Count == 0)
                        {
                            ShellcodeStatus = $"No code caves >= {shellcode.Length} bytes found.";
                            return;
                        }

                        var cave = caves[0];
                        PEWriterService.PatchBytes(FileBytes, cave.offset, shellcode);

                        if (ShellcodeRedirectEp)
                        {
                            // Calculate RVA of cave and set EP
                            // Find which section the cave is in
                            foreach (var section in Sections)
                            {
                                if (cave.offset >= section.RawDataPointer &&
                                    cave.offset < section.RawDataPointer + section.RawDataSize)
                                {
                                    uint rva = (uint)(cave.offset - section.RawDataPointer + section.VirtualAddress);
                                    PEWriterService.ChangeEntryPoint(FileBytes, rva);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                    case "AppendLast":
                    {
                        // Extend last section
                        if (Sections.Count == 0)
                        {
                            ShellcodeStatus = "No sections to append to.";
                            return;
                        }

                        var lastSection = Sections[^1];
                        long appendOffset = lastSection.RawDataPointer + lastSection.RawDataSize;

                        // Extend file if needed
                        if (appendOffset + shellcode.Length > FileBytes.Length)
                        {
                            byte[] extended = new byte[appendOffset + shellcode.Length];
                            Array.Copy(FileBytes, extended, FileBytes.Length);
                            FileBytes = extended;
                        }

                        PEWriterService.PatchBytes(FileBytes, appendOffset, shellcode);

                        // Update section sizes
                        int lastIdx = Sections.Count - 1;
                        uint newRawSize = lastSection.RawDataSize + (uint)shellcode.Length;
                        uint newVSize = lastSection.VirtualSize + (uint)shellcode.Length;
                        PEWriterService.ModifySectionProperties(FileBytes, lastIdx,
                            newRawSize: newRawSize, newVirtualSize: newVSize);

                        // Make section executable
                        uint chars = lastSection.Characteristics | 0x60000020;
                        PEWriterService.ModifySectionProperties(FileBytes, lastIdx, newCharacteristics: chars);

                        if (ShellcodeRedirectEp)
                        {
                            uint rva = lastSection.VirtualAddress + lastSection.VirtualSize;
                            PEWriterService.ChangeEntryPoint(FileBytes, rva);
                        }
                        break;
                    }
                }

                _originalFileBytes = null;
                IsModified = true;
                RefreshAllViews();
                ShellcodeStatus = $"Shellcode injected ({shellcode.Length} bytes) via {ShellcodeInjectionMode}.";
            }
            catch (Exception ex)
            {
                ShellcodeStatus = $"Injection failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private void LoadShellcodeFromFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                ShellcodeHex = BitConverter.ToString(data).Replace("-", " ");
                ShellcodeStatus = $"Loaded {data.Length} bytes from {Path.GetFileName(filePath)}.";
            }
            catch (Exception ex)
            {
                ShellcodeStatus = $"Load failed: {ex.Message}";
            }
        }

        #endregion


        #region Refresh / Coordination

        public void RefreshAllViews()
        {
            if (FileBytes == null) return;

            try
            {
                // Re-parse snapshot from modified bytes
                var newSnapshot = SnapshotRefreshService.ParseFromBytes(FileBytes);
                Snapshot = newSnapshot;

                // Update EP display
                EntryPointRaw = newSnapshot.EntryPoint;
                EntryPoint = $"0x{newSnapshot.EntryPoint:X}";
                NewEntryPoint = EntryPoint;
                ResolveEntryPointSection(EntryPointRaw);

                // Refresh sections
                Sections.Clear();
                if (newSnapshot.Sections != null)
                    foreach (var s in newSnapshot.Sections) Sections.Add(s);

                // Refresh imports
                ImportEntries.Clear();
                if (newSnapshot.Imports != null)
                    foreach (var i in newSnapshot.Imports) ImportEntries.Add(i);

                // Re-run disassembly
                RunDisassembly(EntryPointRaw);

                // Notify hex editor to reload stream
                HexStreamRefreshRequested?.Invoke();
            }
            catch
            {
                EditStatus = "Warning: Refresh failed — snapshot may be outdated.";
            }
        }

        public void SwitchToHexAtOffset(long offset)
        {
            ActiveTabIndex = 2;
            HexGoToTarget = offset;
        }

        public void SwitchToDisasmAtRva(int rva)
        {
            ActiveTabIndex = 5;
            RunDisassembly(rva);
        }

        #endregion

        #region Internal

        private void RunDisassembly(int rva)
        {
            DisassembledInstructions.Clear();

            if (FileBytes == null || FileBytes.Length == 0 || Snapshot?.Sections == null)
            {
                DisasmStatus = "No data available";
                return;
            }

            int bitness = Architecture.Contains("x64") || Architecture.Contains("64") ? 64 : 32;
            long imageBase = Snapshot.ImageBase;

            try
            {
                var instructions = DisassemblyService.Disassemble(
                    FileBytes, rva, Snapshot.Sections, bitness, imageBase);

                foreach (var instr in instructions)
                    DisassembledInstructions.Add(instr);

                DisasmStatus = $"{instructions.Count} instructions from RVA 0x{rva:X}";
            }
            catch (Exception ex)
            {
                DisasmStatus = $"Disassembly error: {ex.Message}";
            }
        }



        private void ResolveEntryPointSection(int ep)
        {
            if (Snapshot?.Sections == null)
            {
                EntryPointSection = "Unknown";
                return;
            }

            foreach (var section in Snapshot.Sections)
            {
                if (ep >= section.VirtualAddress &&
                    ep < section.VirtualAddress + section.VirtualSize)
                {
                    EntryPointSection = section.Name;
                    return;
                }
            }

            EntryPointSection = "Outside sections";
        }

        private static string FormatFileSize(long bytes)
        {
            string[] units = ["B", "KB", "MB", "GB"];
            double size = bytes;
            int idx = 0;
            while (size >= 1024 && idx < units.Length - 1)
            {
                size /= 1024;
                idx++;
            }
            return size >= 10 ? $"{size:0.#} {units[idx]}" : $"{size:0.##} {units[idx]}";
        }

        private static string FormatMachine(ushort machine) => machine switch
        {
            0x14C => "i386 (x86)",
            0x8664 => "AMD64 (x64)",
            0xAA64 => "ARM64",
            0x1C0 => "ARM",
            _ => $"0x{machine:X4}"
        };

        private static string FormatSubsystem(ushort subsystem) => subsystem switch
        {
            1 => "Native",
            2 => "Windows GUI",
            3 => "Windows Console",
            5 => "OS/2 Console",
            7 => "POSIX Console",
            10 => "EFI Application",
            _ => $"Unknown ({subsystem})"
        };

        private static string FormatTimestamp(int timestamp)
        {
            try
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                return dt.ToString("yyyy-MM-dd HH:mm:ss UTC");
            }
            catch
            {
                return $"0x{timestamp:X8}";
            }
        }

        #endregion
    }

    public class ExtractedString
    {
        public string Offset { get; set; } = "";
        public string Value { get; set; } = "";
        public int Length { get; set; }
        public string Encoding { get; set; } = "ASCII";
        public long OffsetRaw { get; set; }
    }
}
